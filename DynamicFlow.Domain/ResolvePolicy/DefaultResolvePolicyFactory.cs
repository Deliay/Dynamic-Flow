namespace DynamicFlow.Domain.ResolvePolicy
{
    public class DefaultResolvePolicyFactory
    {
        public class All<T> : IResolvePolicy<T> where T : DependencyTask<T>
        {
            public ValueTask<bool> CanResolve(T instance)
            {
                return ValueTask.FromResult(instance.Dependencies.All(d => d.CurrentState == TaskState.Completed));
            }

            public readonly static All<T> Default = new();
        }

        public class Or<T> : IResolvePolicy<T> where T : DependencyTask<T>
        {
            public ValueTask<bool> CanResolve(T instance)
            {
                return ValueTask.FromResult(instance.Dependencies.Any(d => d.CurrentState == TaskState.Completed));
            }
            public readonly static Or<T> Default = new();
        }

        public class Empty<T> : IResolvePolicy<T> where T : DependencyTask<T>
        {
            public ValueTask<bool> CanResolve(T instance)
            {
                return ValueTask.FromResult(true);
            }
            public readonly static Empty<T> Default = new();
        }

        public static IResolvePolicy<T> OfPolicy<T>(DefaultResolvePolicies policy)
            where T : DependencyTask<T>
        {
            return policy switch
            {
                DefaultResolvePolicies.All => All<T>.Default,
                DefaultResolvePolicies.Or => Or<T>.Default,
                DefaultResolvePolicies.Optional => Empty<T>.Default,
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
