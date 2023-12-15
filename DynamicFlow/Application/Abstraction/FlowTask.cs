using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels.DefaultMetadata;
using DynamicFlow.Domain.ResolvePolicy;

namespace DynamicFlow.Application.Abstraction;

public class FlowTask(string id, DefaultResolvePolicies resolvePolicy) : LabeledTask<FlowTask>
{
    public string Id => id;

    public DateTime PausedAt { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public DateTime FailedAt { get; private set; }

    public DateTime RollbackAt { get; private set; }

    public DateTime UnlockedAt { get; private set; }

    public DefaultResolvePolicies ResolvePolicy => resolvePolicy;

    public override IResolvePolicy<FlowTask> GetResolvePolicy()
    {
        return DefaultResolvePolicyFactory.OfPolicy<FlowTask>(ResolvePolicy);
    }

    public async ValueTask<DueTo> GetDueTo()
    {
        if (await Contains(DynFlow.Due))
        {
            return new DueTo(DateTime.Parse((await Get(DynFlow.Due))!));
        }

        var duration = int.Parse((await Get(DynFlow.Duration))!);

        // 有依赖
        if (Dependencies.Count != 0)
        {
            var policy = GetResolvePolicy();
            // 依赖任务未完成
            if (!await policy.CanResolve(this))
            {
                // TBD
                return DueTo.TBD;
            }
        }

        // 无依赖，或依赖完成，则是开始时间+duration
        return new DueTo(StartedAt + TimeSpan.FromDays(duration));
    }

    protected override ValueTask TaskStarted()
    {
        StartedAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TaskPaused()
    {
        PausedAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TaskCompleted()
    {
        CompletedAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TaskFailed()
    {
        FailedAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TaskRollback()
    {
        RollbackAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask TaskUnlocked()
    {
        UnlockedAt = DateTime.Now;
        return ValueTask.CompletedTask;
    }

}
