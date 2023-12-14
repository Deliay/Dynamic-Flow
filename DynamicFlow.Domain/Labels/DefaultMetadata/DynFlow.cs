namespace DynamicFlow.Domain.Labels.DefaultMetadata
{
    public static class DynFlow
    {
        public const string Namespace = "dynflow";
        public readonly static LabelMetadata Id = new(Namespace, "id", 1);
        public readonly static LabelMetadata Name = new(Namespace, "name", 1);
        public readonly static LabelMetadata Description = new(Namespace, "description", 1);
        public readonly static LabelMetadata Due = new(Namespace, "due", 1);
        public readonly static LabelMetadata Duration = new(Namespace, "duration", 1);
        public readonly static LabelMetadata Extends = new(Namespace, "extends", 0);
        public readonly static LabelMetadata Spread = new(Namespace, "spread", 0);
        public readonly static LabelMetadata Assignee = new(Namespace, "assignee", 0);
        public readonly static LabelMetadata Overtime = new(Namespace, "overtime", 1);
        public readonly static LabelMetadata Group = new(Namespace, "group", 0);
        public readonly static LabelMetadata Empty = new(Namespace, "empty", 0);
    }
}
