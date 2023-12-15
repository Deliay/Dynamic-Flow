using Dapr.Client;
using DynamicFlow.Application.Abstraction;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Application.Repository;
using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.Labels.DefaultMetadata;
using DynamicFlow.Infrastruction.Repository;
using DynamicFlow.Infrastruction.Util;
using MongoDB.Driver;

namespace DynamicFlow.Application.View;

public class DynamicForest(DaprClient daprClient, DaprLabeledTreeRepository treeRepository, DaprLabeledTaskRespitory taskRespitory) : IDisposable
{
    private IMongoCollection<LabeledTaskObject> Tasks => taskRespitory.Collection;

    public event TreeCreatedEvent? OnTreeCreated;
    public event TreeTaskAddedEvent? OnTreeTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<FlowTask>? OnLabelUpdated;
    public event LabelAppliedEvent<FlowTask>? OnLabelApplied;
    public event TaskStatusUpdatedEvent<FlowTask>? OnTreeNodeUpdate;

    private readonly Dictionary<string, FlowTree> trees = [];        

    private void AttachEvent(FlowTree tree)
    {

        tree.OnTaskDependencyUpdated += Tree_OnTaskDependencyUpdated;
        tree.OnTaskAdded += Tree_OnTaskAdded;
        tree.OnLabelApplied += Tree_OnLabelApplied;
        tree.OnLabelUpdated += Tree_OnLabelUpdated;
        tree.OnNodeUpdate += Tree_OnNodeUpdate;

        // WIP: dependency relationship update
    }

    public async ValueTask<FlowTree> CreateOrGet(string Name)
    {
        var tree = new DynamicTree(daprClient, treeRepository.Collection, Guid.NewGuid().ToString());
        await tree.Add(new(DynFlow.Name, Name));
        trees.Add(tree.Id, tree);

        AttachEvent(tree);

        await (OnTreeCreated?.Invoke(tree) ?? ValueTask.CompletedTask);
        return tree;
    }

    private async ValueTask Tree_OnNodeUpdate(FlowTask task, TaskState prev, TaskState next)
    {
        await daprClient.BeginDaprLock(nameof(DynamicTask), task.Id, async () => 
        {
            var currenState = await Tasks.SelectId(task.Id).Project(task => task.State).SingleAsync();
            if (currenState != prev) throw new InvalidOperationException($"task {task.Id} state not match, prev={prev}, current={currenState}");

            var res = await Tasks.UpdateOneAsync(Tasks.Filter(task.Id), Tasks.Update().Set(task => task.State, next));

            if (!res.IsModifiedCountAvailable || res.ModifiedCount == 0) throw new InvalidOperationException("No task updated");
        });
        await (OnTreeNodeUpdate?.Invoke(task, prev, next) ?? ValueTask.CompletedTask);
    }

    private async ValueTask Tree_OnLabelUpdated(FlowTask task, Label oldLabel, Label newLabel)
    {
        if (oldLabel.Id != newLabel.Id) throw new InvalidOperationException("New label Id not equals to oldLabel");

        await daprClient.BeginDaprLock(nameof(DynamicTask), task.Id, async () => 
        {
            await Tasks.UpdateOneAsync(Tasks.Filter(task.Id), Tasks.Update().PullFilter(task => task.Labels, l => l.Id == oldLabel.Id));
            await Tasks.UpdateOneAsync(Tasks.Filter(task.Id), Tasks.Update().Push(task => task.Labels, newLabel));
        });
        await (OnLabelUpdated?.Invoke(task, oldLabel, newLabel) ?? ValueTask.CompletedTask);
    }

    private async ValueTask Tree_OnLabelApplied(FlowTask task, Label label)
    {
        await daprClient.BeginDaprLock(nameof(DynamicTask), task.Id, async () => 
        {
            var exist = await Tasks.Find(Tasks.Filter().ElemMatch(task => task.Labels, l => l.Id == label.Id)).AnyAsync();
            if (exist) throw new InvalidOperationException($"Label {label.Id} already exists");

            await Tasks.UpdateOneAsync(Tasks.Filter(task.Id), Tasks.Update().Push(task => task.Labels, label));
        });
        await (OnLabelApplied?.Invoke(task, label) ?? ValueTask.CompletedTask);
    }

    private async ValueTask Tree_OnTaskAdded(FlowTask flowTask)
    {
        if (flowTask is not DynamicTask task) throw new InvalidOperationException("Non dynamic task instance detected");
        await daprClient.BeginDaprLock(nameof(DynamicTask), flowTask.Id, async () =>
        {
            var exist = await Tasks.Find(Tasks.Filter(flowTask.Id)).AnyAsync();
            if (exist) throw new InvalidOperationException($"Task {flowTask.Id} already exists");

            await Tasks.InsertOneAsync(LabeledTaskObject.From(task));
        });
        await (OnTreeTaskAdded?.Invoke(flowTask) ?? ValueTask.CompletedTask);
    }

    private async ValueTask Tree_OnTaskDependencyUpdated(FlowTask beenResolvedTask, FlowTask resolverTask)
    {
        await (OnTaskDependencyUpdated?.Invoke(beenResolvedTask, resolverTask) ?? ValueTask.CompletedTask);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var tree in trees.Values)
        {
            tree.OnTaskDependencyUpdated -= Tree_OnTaskDependencyUpdated;
            tree.OnTaskAdded -= Tree_OnTaskAdded;
            tree.OnLabelApplied -= Tree_OnLabelApplied;
            tree.OnLabelUpdated -= Tree_OnLabelUpdated;
            tree.OnNodeUpdate -= Tree_OnNodeUpdate;
        }
    }
}
