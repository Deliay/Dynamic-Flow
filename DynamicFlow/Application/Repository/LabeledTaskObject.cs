using DynamicFlow.Application.View;
using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.ResolvePolicy;

namespace DynamicFlow.Application.Repository;

public record LabeledTaskObject(
    string Id,
    List<Label> Labels,
    List<string> Dependencies,
    List<string> References,
    TaskState State,
    DateTime PausedAt,
    DateTime StartedAt,
    DateTime CompletedAt,
    DateTime FailedAt,
    DateTime RollbackAt,
    DateTime UnlockedAt,
    DefaultResolvePolicies ResolvePolicy)
{
    public static LabeledTaskObject From(DynamicTask task) =>
        new(
            task.Id,
            task.GetAllLabels().ToList(),
            task.Dependencies.Select(t => t.Id).ToList(),
            task.References.Select(t => t.Id).ToList(),
            task.CurrentState,
            task.PausedAt,
            task.StartedAt,
            task.CompletedAt,
            task.FailedAt,
            task.RollbackAt,
            task.UnlockedAt,
            task.ResolvePolicy);
}