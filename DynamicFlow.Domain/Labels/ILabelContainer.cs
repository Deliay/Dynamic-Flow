namespace DynamicFlow.Domain.Labels
{
    public interface ILabelContainer
    {
        ValueTask<string?> Get(LabelMetadata metadata);

        ValueTask<Label?> Find(LabelMetadata metadata);

        ValueTask<IReadOnlySet<Label>?> FindAll(LabelMetadata metadata);

        ValueTask<bool> Add(Label label);

        ValueTask<bool> AddOrUpdate(Label label);

        ValueTask<bool> Remove(Label label);

        ValueTask<bool> RemoveAll(LabelMetadata metadata);

        ValueTask<bool> Contains(LabelMetadata metadata);
    }
}
