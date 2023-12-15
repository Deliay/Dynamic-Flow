using DynamicFlow.Domain;
using DynamicFlow.Domain.Labels;

namespace DynamicFlow.Application.Repository;

public record LabeledTreeObject(string Id, List<Label> Labels, HashSet<string> TaskIds);