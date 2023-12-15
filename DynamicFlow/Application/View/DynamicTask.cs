
using DynamicFlow.Application.Abstraction;
using DynamicFlow.Domain.ResolvePolicy;

namespace DynamicFlow.Application.View;

public class DynamicTask(string id, DefaultResolvePolicies resolvePolicy) : FlowTask(id, resolvePolicy)
{
}