namespace DynamicFlow.Application.Abstraction.Event;

public delegate ValueTask TreeTaskAddedEvent(FlowTask task);

public delegate ValueTask TreeTaskDependencyUpdated(FlowTask beenResolvedTask, FlowTask resolverTask);

public delegate ValueTask TreeCreatedEvent(FlowTree node);
