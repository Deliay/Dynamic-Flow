using DynamicFlow.Domain;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Domain.ResolvePolicy;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Abstraction;

public abstract class FlowTree(string treeId) : IDisposable, ILabelContainer
{
    public string Id { get; } = treeId;

    protected readonly Dictionary<string, HashSet<Label>> Labels = [];
    protected readonly Dictionary<string, Label> LabelMapping = [];

    private readonly Dictionary<string, FlowTask> nodes = new()
    {
        { treeId, new FlowTask(treeId, DefaultResolvePolicies.Optional) },
    };

    private FlowTask Root => nodes[Id];

    public event TreeTaskAddedEvent? OnTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<FlowTask>? OnLabelUpdated;
    public event LabelAppliedEvent<FlowTask>? OnLabelApplied;
    public event DependencyStatusUpdatedEvent<FlowTask>? OnNodeUpdate;

    private async ValueTask<FlowTask> _CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        if (nodes.ContainsKey(id)) {
            throw new InvalidOperationException($"'{id}' was duplicated.");
        }

        nodes.Add(id, new(id, resolvePolicy));
        var task = nodes[id];

        task.OnDependencyChanged += Task_OnDependencyChanged;
        task.OnLabelApplied += Task_OnLabelApplied;
        task.OnLabelUpdated += Task_OnLabelUpdated;
        
        await (OnTaskAdded?.Invoke(task) ?? ValueTask.CompletedTask);

        return task;
    }

    public ValueTask<FlowTask> CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        return CreateTask(Root, id, resolvePolicy);
    }

    public ValueTask<FlowTask> CreateTask(string resolver, string id, DefaultResolvePolicies resolvePolicy)
    {
        return CreateTask(nodes[resolver], id, resolvePolicy);
    }

    public async ValueTask<FlowTask> CreateTask(FlowTask resolver, string id, DefaultResolvePolicies resolvePolicy)
    {
        var task = await _CreateTask(id, resolvePolicy);

        await task.ResolveBy(resolver);

        return task;
    }

    private ValueTask Task_OnLabelUpdated(FlowTask task, Label oldLabel, Label newLabel)
    {
        return OnLabelUpdated?.Invoke(task, oldLabel, newLabel) ?? ValueTask.CompletedTask;
    }

    private ValueTask Task_OnLabelApplied(FlowTask task, Label label)
    {
        return OnLabelApplied?.Invoke(task, label) ?? ValueTask.CompletedTask;
    }

    private ValueTask Task_OnDependencyChanged(FlowTask dependency, TaskState prev, TaskState next)
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

    public abstract ValueTask<string?> Get(LabelMetadata metadata);
    public abstract ValueTask<Label?> Find(LabelMetadata metadata);
    public abstract ValueTask<IReadOnlySet<Label>?> FindAll(LabelMetadata metadata);
    public abstract ValueTask<bool> Add(Label label);
    public abstract ValueTask<bool> AddOrUpdate(Label label);
    public abstract ValueTask<bool> Remove(Label label);
    public abstract ValueTask<bool> RemoveAll(LabelMetadata metadata);
    public abstract ValueTask<bool> Contains(LabelMetadata metadata);
}