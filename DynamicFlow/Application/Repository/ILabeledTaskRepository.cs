using System.Runtime.CompilerServices;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Repository;

public interface ILabeledTaskRepository
{
    // public ValueTask<string> GetMetadata(string task, LabelMetadata metadata, CancellationToken cancellationToken);
    // public ValueTask<Label> GetLabel(string task, LabelMetadata metadata, CancellationToken cancellationToken);
    // public IAsyncEnumerable<Label> GetAllLabel(string task, LabelMetadata metadata, CancellationToken cancellationToken);
    // public ValueTask<bool> AddLabel(string task, Label label, CancellationToken cancellationToken);
    // public ValueTask<bool> AddOrUpdate(string task, Label label, CancellationToken cancellationToken);
    // public ValueTask<bool> Remove(string task, Label label, CancellationToken cancellationToken);
    // public ValueTask<bool> RemoveAll(string task, LabelMetadata label, CancellationToken cancellationToken);
    // public ValueTask<bool> Contains(string task, LabelMetadata label, CancellationToken cancellationToken);

    public ValueTask Save(LabeledTaskObject rawTask, CancellationToken cancellationToken);

    public ValueTask<LabeledTaskObject> Get(string id, CancellationToken cancellationToken);

    public IAsyncEnumerable<LabeledTaskObject> Search(LabelMetadata metadata, List<string> values, CancellationToken cancellationToken);
}