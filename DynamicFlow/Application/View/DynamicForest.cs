using DynamicFlow.Application.Abstraction;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.Labels.DefaultMetadata;

namespace DynamicFlow.Application.View;

public class DynamicForest : IDisposable
{
    public event TreeCreatedEvent? OnTreeCreated;
    public event TreeTaskAddedEvent? OnTreeTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<FlowTask>? OnLabelUpdated;
    public event LabelAppliedEvent<FlowTask>? OnLabelApplied;
    public event DependencyStatusUpdatedEvent<FlowTask>? OnTreeNodeUpdate;

    private readonly Dictionary<string, FlowTree> trees = [];        

    public ValueTask<FlowTree> Create(string Name)
    {
        var tree = new DynamicTree(Guid.NewGuid().ToString(), Name);
        tree.Add(new(DynFlow.Name))
        trees.Add(tree.Id, tree);


        tree.OnTaskDependencyUpdated += Tree_OnTaskDependencyUpdated;
        tree.OnTaskAdded += Tree_OnTaskAdded;
        tree.OnLabelApplied += Tree_OnLabelApplied;
        tree.OnLabelUpdated += Tree_OnLabelUpdated;
        tree.OnNodeUpdate += Tree_OnNodeUpdate;

        OnTreeCreated?.Invoke(tree);
        return ValueTask.FromResult(tree);
    }

    private ValueTask Tree_OnNodeUpdate(FlowTask dependency, TaskState prev, TaskState next)
    {
        return OnTreeNodeUpdate?.Invoke(dependency, prev, next) ?? ValueTask.CompletedTask;
    }

    private ValueTask Tree_OnLabelUpdated(FlowTask task, Label oldLabel, Label newLabel)
    {
        return OnLabelUpdated?.Invoke(task, oldLabel, newLabel) ?? ValueTask;
    }

    private ValueTask Tree_OnLabelApplied(FlowTask task, Label label)
    {
        return OnLabelApplied?.Invoke(task, label) ?? ValueTask.CompletedTask;
    }

    private ValueTask Tree_OnTaskAdded(FlowTask node)
    {
        return OnTreeTaskAdded?.Invoke(node) ?? ValueTask.CompletedTask;
    }

    private ValueTask Tree_OnTaskDependencyUpdated(FlowTask beenResolvedTask, FlowTask resolverTask)
    {
        return OnTaskDependencyUpdated?.Invoke(beenResolvedTask, resolverTask) ?? ValueTask.CompletedTask;
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
