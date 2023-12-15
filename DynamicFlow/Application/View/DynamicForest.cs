using DynamicFlow.Application.Abstraction;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Domain;

namespace DynamicFlow.Application.View;

public class DynamicForest : IDisposable
{
    public event TreeCreatedEvent? OnTreeCreated;
    public event TreeTaskAddedEvent? OnTreeTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<DynamicTask>? OnLabelUpdated;
    public event LabelAppliedEvent<DynamicTask>? OnLabelApplied;
    public event DependencyStatusUpdatedEvent<DynamicTask>? OnTreeNodeUpdate;

    private readonly Dictionary<string, DynamicTree> trees = [];        

    public ValueTask<DynamicTree> Create(string Name)
    {
        var tree = new DynamicTree(Guid.NewGuid().ToString(), Name);
        trees.Add(tree.Id, tree);


        tree.OnTaskDependencyUpdated += Tree_OnTaskDependencyUpdated;
        tree.OnTaskAdded += Tree_OnTaskAdded;
        tree.OnLabelApplied += Tree_OnLabelApplied;
        tree.OnLabelUpdated += Tree_OnLabelUpdated;
        tree.OnNodeUpdate += Tree_OnNodeUpdate;

        OnTreeCreated?.Invoke(tree);
        return ValueTask.FromResult(tree);
    }

    private ValueTask Tree_OnNodeUpdate(DynamicTask dependency, TaskState prev, TaskState next)
    {
        return OnTreeNodeUpdate?.Invoke(dependency, prev, next) ?? ValueTask.CompletedTask;
    }

    private void Tree_OnLabelUpdated(DynamicTask task, Domain.Labels.Label oldLabel, Domain.Labels.Label newLabel)
    {
        OnLabelUpdated?.Invoke(task, oldLabel, newLabel);
    }

    private void Tree_OnLabelApplied(DynamicTask task, Domain.Labels.Label label)
    {
        OnLabelApplied?.Invoke(task, label);
    }

    private void Tree_OnTaskAdded(DynamicTask node)
    {
        OnTreeTaskAdded?.Invoke(node);
    }

    private void Tree_OnTaskDependencyUpdated(DynamicTask beenResolvedTask, DynamicTask resolverTask)
    {
        OnTaskDependencyUpdated?.Invoke(beenResolvedTask, resolverTask);
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
