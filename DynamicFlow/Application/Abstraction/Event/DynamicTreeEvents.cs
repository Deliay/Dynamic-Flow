namespace DynamicFlow.Application.Abstraction.Event;

public delegate void TreeTaskAddedEvent(DynamicTask task);

public delegate void TreeTaskDependencyUpdated(DynamicTask beenResolvedTask, DynamicTask resolverTask);

public delegate void TreeCreatedEvent(DynamicTree node);
