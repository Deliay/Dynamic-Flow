namespace DynamicFlow.Domain
{
    public delegate ValueTask TaskStatusUpdatedEvent<T>(T dependency, TaskState prev, TaskState next);

    public delegate ValueTask TaskReferenceRelationUpdatedEvent<T>(T self, T target);

}
