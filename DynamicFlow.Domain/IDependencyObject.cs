using DynamicFlow.Domain.ResolvePolicy;

namespace DynamicFlow.Domain
{
    public interface IDependencyObject<T> where T : IDependencyObject<T>
    {
        public List<T> Dependencies { get; }

        public IResolvePolicy<T> GetResolvePolicy();

        public abstract event DependencyStatusUpdatedEvent<T>? OnDependencyChanged;
    }
}
