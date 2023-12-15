using DynamicFlow.Domain;
using DynamicFlow.Application.Abstraction.Event;
using DynamicFlow.Domain.ResolvePolicy;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Abstraction;

public abstract class FlowTree(string treeId) : IDisposable, ILabelContainer
{
    public string Id { get; } = treeId;

    protected abstract Dictionary<string, FlowTask> Nodes { get; }

    public event TreeTaskAddedEvent? OnTaskAdded;
    public event TreeTaskDependencyUpdated? OnTaskDependencyUpdated;
    public event LabelUpdatedEvent<FlowTask>? OnLabelUpdated;
    public event LabelAppliedEvent<FlowTask>? OnLabelApplied;
    public event TaskStatusUpdatedEvent<FlowTask>? OnNodeUpdate;
    public event TaskReferenceRelationUpdatedEvent<FlowTask>? OnTaskReferenceAdded;
    public event TaskReferenceRelationUpdatedEvent<FlowTask>? OnTaskReferenceRemoved;
    public event TaskReferenceRelationUpdatedEvent<FlowTask>? OnTaskDependencyAdded;
    public event TaskReferenceRelationUpdatedEvent<FlowTask>? OnTaskDependencyRemoved;

    private void AttachEvent(FlowTask task)
    {
        task.OnDependencyChanged += Task_OnTaskStatusChanged;
        task.OnLabelApplied += Task_OnLabelApplied;
        task.OnLabelUpdated += Task_OnLabelUpdated;
        task.OnDependencyAdded += OnTaskDependencyAdded;
        task.OnDependencyRemoved += OnTaskDependencyRemoved;
        task.OnReferenceAdded += OnTaskReferenceAdded;
        task.OnReferenceRemoved += OnTaskReferenceRemoved;
    }

    public virtual async ValueTask<FlowTask> CreateTask(string id, DefaultResolvePolicies resolvePolicy)
    {
        if (Nodes.ContainsKey(id)) {
            throw new InvalidOperationException($"'{id}' was duplicated.");
        }

        Nodes.Add(id, new(id, resolvePolicy));
        var task = Nodes[id];

        AttachEvent(task);
        
        await (OnTaskAdded?.Invoke(task) ?? ValueTask.CompletedTask);
        return task;
    }

    public async ValueTask<FlowTask> CreateTask(string resolverId, string id, DefaultResolvePolicies resolvePolicy)
    {
        if (!Nodes.TryGetValue(resolverId, out FlowTask? resolver)) throw new InvalidOperationException($"Task {resolverId} not loaded to this tree");
        var task = await CreateTask(id, resolvePolicy);

        await task.ResolveBy(resolver);

        return task;
    }

    public ValueTask<FlowTask> CreateTask(FlowTask resolver, string id, DefaultResolvePolicies resolvePolicy)
    {
        return CreateTask(resolver.Id, id, resolvePolicy);
    }

    private ValueTask Task_OnLabelUpdated(FlowTask task, Label oldLabel, Label newLabel)
    {
        return OnLabelUpdated?.Invoke(task, oldLabel, newLabel) ?? ValueTask.CompletedTask;
    }

    private ValueTask Task_OnLabelApplied(FlowTask task, Label label)
    {
        return OnLabelApplied?.Invoke(task, label) ?? ValueTask.CompletedTask;
    }

    private ValueTask Task_OnTaskStatusChanged(FlowTask task, TaskState prev, TaskState next)
    {
        var res = OnNodeUpdate?.Invoke(task, prev, next);
        return res ?? ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var node in Nodes.Values)
        {
            node.OnDependencyChanged -= Task_OnTaskStatusChanged;
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
    public abstract ValueTask<int> Count(LabelMetadata metadata);
}