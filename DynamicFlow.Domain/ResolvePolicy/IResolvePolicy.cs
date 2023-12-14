namespace DynamicFlow.Domain.ResolvePolicy
{
    public interface IResolvePolicy<T>
    {
        ValueTask<bool> CanResolve(T instance);
    }
}
