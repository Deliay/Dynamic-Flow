using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapr.Client;
using DynamicFlow.Application.Repository;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.Labels.DefaultMetadata;
using DynamicFlow.Infrastruction.Util;
using Microsoft.AspNetCore.Http.Features;
using MongoDB.Driver;

namespace DynamicFlow.Infrastruction.Repository;

public class DaprLabeledTreeRepository(IMongoRespository Db) : ITreeRespository
{
    public IMongoCollection<LabeledTreeObject> Collection => Db.Trees;

    private const string TreeDaprStore = "flow_tree_" + nameof(DaprLabeledTaskRespitory);

    public IAsyncEnumerable<LabeledTreeObject> GetAllTree(CancellationToken cancellationToken)
    {
        return Db.Trees.Select(where => where.Empty).ToAsyncEnumerable(cancellationToken);
    }

    public async ValueTask Save(LabeledTreeObject tree, CancellationToken cancellationToken)
    {
        if (await Db.Trees.Select(where => where.Eq(tree => tree.Id, tree.Id)).AnyAsync(cancellationToken))
            throw new InvalidDataException("Tree.Id duplicated");

        await Db.Trees.InsertOneAsync(tree, cancellationToken: cancellationToken);
    }
}