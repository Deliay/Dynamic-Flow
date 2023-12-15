namespace DynamicFlow.Domain.Labels
{
    public class Label(string id, LabelMetadata metadata, string value)
    {
        public Label(LabelMetadata metadata, string value) : this(Guid.NewGuid().ToString(), metadata, value) {

        }

        public string Id { get; set; } = id;
        public LabelMetadata Metadata { get; set; } = metadata;
        public string Value { get; set; } = value;

        public override string ToString()
        {
            return $"{Metadata}:{Id}:{Value}";
        }

        public override bool Equals(object? obj)
        {
            return obj?.ToString() == ToString();
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
