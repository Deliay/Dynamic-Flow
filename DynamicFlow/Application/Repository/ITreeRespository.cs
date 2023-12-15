using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Repository;

public interface ITreeRespository
{
    public ValueTask Save(LabeledTreeObject tree, CancellationToken cancellationToken);

    public IAsyncEnumerable<LabeledTreeObject> GetAllTree(CancellationToken cancellationToken);
}