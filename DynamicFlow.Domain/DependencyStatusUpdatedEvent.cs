namespace DynamicFlow.Domain
{
    public delegate ValueTask DependencyStatusUpdatedEvent<T>(T dependency, TaskState prev, TaskState next);
}
