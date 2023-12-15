using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Repository;

public record LabeledTaskObject(string Id, List<Label> Labels, List<string> Dependencies, List<string> References, TaskState State);