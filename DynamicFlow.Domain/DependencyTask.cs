using DynamicFlow.Domain.ResolvePolicy;
using System.Security.Cryptography.Xml;

namespace DynamicFlow.Domain
{
    public abstract class DependencyTask<T> : ITask<T>, IDependencyObject<T>, IDisposable where T : DependencyTask<T>
    {
        public List<T> Dependencies { get; } = [];
        public List<T> References { get; } = [];

        public TaskState CurrentState { get; set; } = TaskState.Locked;

        public event TaskStatusUpdatedEvent<T>? OnDependencyChanged;
        public event TaskReferenceRelationUpdatedEvent<T>? OnDependencyAdded;
        public event TaskReferenceRelationUpdatedEvent<T>? OnDependencyRemoved;

        public event TaskReferenceRelationUpdatedEvent<T>? OnReferenceAdded;
        public event TaskReferenceRelationUpdatedEvent<T>? OnReferenceRemoved;

        private async ValueTask RaiseMulitInvocationEvent(TaskState prev, TaskState next)
        {
            if (OnDependencyChanged is not null)
            {
                var invocations = OnDependencyChanged.GetInvocationList();
                foreach (var invocation in invocations)
                {
                    var method = (TaskStatusUpdatedEvent<T>)invocation;
                    await method((T)this, prev, next);
                }
            }
        }

        private async ValueTask RaiseMulitInvocationEvent(TaskReferenceRelationUpdatedEvent<T>? instance, T target)
        {
            if (instance is not null)
            {
                var invocations = instance.GetInvocationList();
                foreach (var invocation in invocations)
                {
                    var method = (TaskReferenceRelationUpdatedEvent<T>)invocation;
                    await method((T)this, target);
                }
            }
        }

        protected virtual ValueTask TaskUnlocked() => ValueTask.CompletedTask;
        protected virtual ValueTask TaskStarted() => ValueTask.CompletedTask;
        protected virtual ValueTask TaskCompleted() => ValueTask.CompletedTask;
        protected virtual ValueTask TaskFailed() => ValueTask.CompletedTask;
        protected virtual ValueTask TaskRollback() => ValueTask.CompletedTask;
        protected virtual ValueTask TaskPaused() => ValueTask.CompletedTask;

        public void ForceUpdateState(TaskState state)
        {
            CurrentState = state;
        }

        public virtual async ValueTask<bool> MoveState(TaskState nextState)
        {
            if (CurrentState == nextState) return false;
            var prevState = CurrentState;
            switch (CurrentState)
            {
                case TaskState.Locked:
                    switch (nextState)
                    {
                        case TaskState.NotStart:
                            CurrentState = nextState;
                            await TaskUnlocked();
                            await RaiseMulitInvocationEvent(prevState, nextState);
                            return true;
                        default:
                            return false;
                    }
                case TaskState.NotStart:
                    switch (nextState)
                    {
                        case TaskState.InProgress:
                        case TaskState.Locked:
                            switch (nextState)
                            {
                                case TaskState.InProgress: await TaskStarted(); break;
                                case TaskState.Locked: await TaskRollback(); break;
                            }
                            CurrentState = nextState;
                            await RaiseMulitInvocationEvent(prevState, nextState);
                            return true;
                        default:
                            return false;
                    }
                case TaskState.InProgress:
                    switch (nextState)
                    {
                        case TaskState.NotStart:
                        case TaskState.Paused:
                        case TaskState.Failed:
                        case TaskState.Completed:
                            switch (nextState)
                            {
                                case TaskState.NotStart: await TaskRollback(); break;
                                case TaskState.Paused: await TaskPaused(); break;
                                case TaskState.Failed: await TaskFailed(); break;
                                case TaskState.Completed: await TaskCompleted(); break;
                            }
                            CurrentState = nextState;
                            await RaiseMulitInvocationEvent(prevState, nextState);
                            return true;
                        default:
                            return false;
                    }
                case TaskState.Completed:
                    switch (nextState)
                    {
                        case TaskState.InProgress:
                            switch (nextState)
                            {
                                case TaskState.InProgress: await TaskRollback(); break;
                            }
                            CurrentState = nextState;
                            await RaiseMulitInvocationEvent(prevState, nextState);
                            return true;
                        default:
                            return false;
                    }
            }
            return false;
        }

        protected virtual async ValueTask DependencyUpdated(T dependency, TaskState prevState, TaskState nextState)
        {
            var policy = GetResolvePolicy();
            var resolved = await policy.CanResolve((T)this);

            if (CurrentState == TaskState.Locked && resolved)
            {
                await MoveState(TaskState.NotStart);
            }
            else if ((CurrentState == TaskState.NotStart) && !resolved)
            {
                await MoveState(TaskState.Locked);
            }
        }

        public static bool TryResolve(T task, HashSet<T> prev)
        {
            var prevSet = prev ?? [task];

            foreach (var depTask in task.Dependencies)
            {
                if (prevSet.Contains(depTask))
                {
                    return false;
                }
                prevSet.Add(depTask);
            }
            return true;
        }

        public virtual async ValueTask Reference(T task)
        {
            References.Add(task);
            await RaiseMulitInvocationEvent(OnReferenceAdded, task);
            await task.DependencyUpdated((T)this, CurrentState, CurrentState);
            
        }
        public virtual async ValueTask DisReference(T task)
        {
            References.Remove(task);
            await RaiseMulitInvocationEvent(OnReferenceRemoved, task);
            await task.DependencyUpdated((T)this, CurrentState, CurrentState);
        }

        public virtual async ValueTask ResolveBy(T task)
        {
            DependencyTask<T>.TryResolve(task, [(T)this]);

            Dependencies.Add(task);
            await RaiseMulitInvocationEvent(OnDependencyAdded, task);
            await task.Reference((T)this);
            task.OnDependencyChanged += DependencyUpdated;
        }

        public virtual async ValueTask DisResolve(T task)
        {
            if (!Dependencies.Contains(task))
            {
                return;
            }
            Dependencies.Remove(task);
            await RaiseMulitInvocationEvent(OnDependencyRemoved, task);
            await DisReference(task);
            task.OnDependencyChanged -= DependencyUpdated;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            foreach (var dependency in Dependencies)
            {
                dependency.OnDependencyChanged -= DependencyUpdated;
            }
            foreach (var reference in References)
            {
                OnDependencyChanged -= reference.OnDependencyChanged;
                reference.Dependencies.Remove((T)this);
            }
        }

        public abstract IResolvePolicy<T> GetResolvePolicy();
    }

}
