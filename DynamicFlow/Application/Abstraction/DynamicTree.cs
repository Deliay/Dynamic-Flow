using DynamicFlow.Domain;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Domain.ResolvePolicy;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Abstraction;

public class DynamicTree(string id, string name) : IDisposable, ILabelContainer
{
    public string Id => id;

    public string Name => name;
    
    private readonly Dictionary<string, HashSet<Label>> _labels;
    private readonly Dictionary<string, Label> _labelMapping;

    private readonly Dictionary<string, DynamicTask> nodes = new()
    {
        { id, new DynamicTask(id, DefaultResolvePolicies.Optional) },
    };

    private DynamicTask Root => nodes[id];

    public event TreeTaskAddedEvent? OnTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<DynamicTask>? OnLabelUpdated;
    public event LabelAppliedEvent<DynamicTask>? OnLabelApplied;
    public event DependencyStatusUpdatedEvent<DynamicTask>? OnNodeUpdate;

    private async ValueTask<DynamicTask> _CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        if (nodes.ContainsKey(id)) {
            throw new InvalidOperationException($"'{id}' was duplicated.");
        }

        nodes.Add(id, new(id, resolvePolicy));
        var task = nodes[id];

        task.OnDependencyChanged += Task_OnDependencyChanged;
        task.OnLabelApplied += Task_OnLabelApplied;
        task.OnLabelUpdated += Task_OnLabelUpdated;
        
        OnTaskAdded?.Invoke(task);

        return task;
    }

    public ValueTask<DynamicTask> CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        return CreateTask(Root, id, resolvePolicy);
    }

    public ValueTask<DynamicTask> CreateTask(string resolver, string id, DefaultResolvePolicies resolvePolicy)
    {
        return CreateTask(nodes[resolver], name, resolvePolicy);
    }

    public async ValueTask<DynamicTask> CreateTask(DynamicTask resolver, string id, DefaultResolvePolicies resolvePolicy)
    {
        var task = await _CreateTask(id, resolvePolicy);

        await task.ResolveBy(resolver);

        return task;
    }

    private void Task_OnLabelUpdated(DynamicTask task, Label oldLabel, Label newLabel)
    {
        OnLabelUpdated?.Invoke(task, oldLabel, newLabel);
    }

    private void Task_OnLabelApplied(DynamicTask task, Label label)
    {
        OnLabelApplied?.Invoke(task, label);
    }

    private ValueTask Task_OnDependencyChanged(DynamicTask dependency, TaskState prev, TaskState next)
    {
        var res = OnNodeUpdate?.Invoke(dependency, prev, next);
        return res ?? ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var node in nodes.Values)
        {
            node.OnDependencyChanged -= Task_OnDependencyChanged;
            node.OnLabelUpdated -= Task_OnLabelUpdated;
            node.OnLabelApplied -= Task_OnLabelApplied;
        }
    }

    public ValueTask<string?> Get(LabelMetadata metadata)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Label?> Find(LabelMetadata metadata)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IReadOnlySet<Label>?> FindAll(LabelMetadata metadata)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> Add(Label label)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> AddOrUpdate(Label label)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> Remove(Label label)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemoveAll(LabelMetadata metadata)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> Contains(LabelMetadata metadata)
    {
        throw new NotImplementedException();
    }
}