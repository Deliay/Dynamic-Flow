namespace DynamicFlow.Domain
{
    public interface ITask<T> where T : ITask<T>
    {
        public TaskState CurrentState { get; }

        public ValueTask<bool> MoveState(TaskState state);

        public ValueTask ResolveBy(T task);
    }
}
