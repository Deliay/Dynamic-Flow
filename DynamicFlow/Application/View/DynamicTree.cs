using Dapr.Client;
using DynamicFlow.Application.Abstraction;
using DynamicFlow.Application.Repository;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.ResolvePolicy;
using DynamicFlow.Infrastruction.Util;
using MongoDB.Driver;

namespace DynamicFlow.Application.View;

public class DynamicTree(DaprClient dapr, IMongoCollection<LabeledTreeObject> trees, string id) : FlowTree(id)
{
    protected override Dictionary<string, FlowTask> Nodes { get; } = [];

    public override async ValueTask<FlowTask> CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        var task = await base.CreateTask(id, resolvePolicy);
        await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().AddToSet(tree => tree.TaskIds, task.Id));
        return task;
    }

    public override async ValueTask<bool> Add(Label label)
    {
        return await dapr.BeginDaprLock(nameof(DynamicTree), Id, async () =>
        {   
            var treeLabels = await trees.SelectId(Id).Project(tree => tree.Labels).SingleAsync();
            if (label.Metadata.AllowCount > 0)
            {
                var current = treeLabels.Count(l => l.Metadata == label.Metadata);
                if (current >= label.Metadata.AllowCount) return false;
            }
            var alreadyExist = await trees.Find(trees.Filter().ElemMatch(tree => tree.Labels, l => l.Id == label.Id)).AnyAsync();
            if (alreadyExist) return false;

            treeLabels.Add(label);
            var res = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().Push(tree => tree.Labels, label));
            return res.IsModifiedCountAvailable && res.ModifiedCount > 0;
        });
    }

    public override async ValueTask<bool> AddOrUpdate(Label label)
    {
        return await dapr.BeginDaprLock(nameof(DynamicTree), Id, async () =>
        {   
            var treeLabels = await trees.SelectId(Id).Project(tree => tree.Labels).SingleAsync();
            if (label.Metadata.AllowCount > 0)
            {
                var current = treeLabels.Count(l => l.Metadata == label.Metadata);
                if (current >= label.Metadata.AllowCount)
                {
                    var update = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().Set(tree => tree.Labels, [label]));
                    return update.IsModifiedCountAvailable && update.ModifiedCount > 0;
                }
            }
            var alreadyExist = await trees.Find(trees.Filter().ElemMatch(tree => tree.Labels, l => l.Id == label.Id)).AnyAsync();
            if (alreadyExist)
            {
                treeLabels.RemoveAll(l => l.Id == label.Id);
                treeLabels.Add(label);
                var update = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().Set(tree => tree.Labels, treeLabels));
                return update.IsModifiedCountAvailable && update.ModifiedCount > 0;
            }
            treeLabels.Add(label);
            var res = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().Push(tree => tree.Labels, label));
            return res.IsModifiedCountAvailable && res.ModifiedCount > 0;
        });
    }

    public override async ValueTask<int> Count(LabelMetadata metadata)
    {
        return (await trees.Find(trees.Filter(Id)).Project(tree => tree.Labels).FirstOrDefaultAsync()).Count;
    }

    public override async ValueTask<Label?> Find(LabelMetadata metadata)
    {
        return (await trees.Find(trees.Filter().ElemMatch(tree => tree.Labels, label => label.Metadata == metadata))
            .Project(tree => tree.Labels).SingleAsync()).FirstOrDefault();
    }

    public override async ValueTask<IReadOnlySet<Label>?> FindAll(LabelMetadata metadata)
    {
        return (await trees.Find(trees.Filter().ElemMatch(tree => tree.Labels, label => label.Metadata == metadata))
            .Project(tree => tree.Labels).SingleAsync()).ToHashSet();
    }

    public override async ValueTask<string?> Get(LabelMetadata metadata)
    {
        return (await Find(metadata))?.Value;
    }

    public override async ValueTask<bool> Remove(Label label)
    {
        return await dapr.BeginDaprLock(nameof(DynamicTree), Id, async () =>
        {   
            var res = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().PullFilter(tree => tree.Labels, label => label.Id == label.Id));
            return res.IsModifiedCountAvailable && res.ModifiedCount > 0;
        });
    }

    public override async ValueTask<bool> RemoveAll(LabelMetadata metadata)
    {
        return await dapr.BeginDaprLock(nameof(DynamicTree), Id, async () =>
        {   
            var res = await trees.UpdateOneAsync(trees.Filter(Id), trees.Update().PullFilter(tree => tree.Labels, label => label.Metadata == metadata));
            return res.IsModifiedCountAvailable && res.ModifiedCount > 0;
        });
    }
}