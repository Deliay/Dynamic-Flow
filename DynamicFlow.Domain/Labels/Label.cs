namespace DynamicFlow.Domain.Labels
{
    public class Label(int id, LabelMetadata metadata, string value)
    {
        public int Id { get; set; } = id;
        public LabelMetadata Metadata { get; set; } = metadata;
        public string Value { get; set; } = value;

        public override string ToString()
        {
            return $"{Metadata}:{Value}";
        }
    }
}
