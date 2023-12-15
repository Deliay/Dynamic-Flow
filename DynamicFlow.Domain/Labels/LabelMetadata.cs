using System.ComponentModel;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DynamicFlow.Domain.Labels
{
    [JsonConverter(typeof(LabelMetadataConverter))]
    public readonly struct LabelMetadata(string @namespace, string name, int allowCount) : IEqualityOperators<LabelMetadata, LabelMetadata, bool>
    {
        public string Namespace { get; } = @namespace;

        public string Name { get; } = name;

        /// <summary>
        /// 允许数量，0为无限制
        /// </summary>
        public int AllowCount { get; } = allowCount;

        public override readonly string ToString()
        {
            return $"{AllowCount}/{Namespace}/{Name}";
        }

        public static bool operator ==(LabelMetadata left, LabelMetadata right)
        {
            return left.ToString() == right.ToString();
        }

        public static bool operator !=(LabelMetadata left, LabelMetadata right)
        {
            return left.ToString() != right.ToString();
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

    public class LabelMetadataConverter : JsonConverter<LabelMetadata>
    {

        public override LabelMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString() ?? throw new InvalidDataException("Unknown metadata");
            var args = str.Split('/');
            if (args.Length == 2)
            {
                return new LabelMetadata(args[0], args[1], 0);
            }
            else if (args.Length == 3)
            {
                return new LabelMetadata(args[1], args[2], int.Parse(args[0]));
            }
            throw new InvalidDataException($"Unknown metadata {str}");
        }

        public override void Write(Utf8JsonWriter writer, LabelMetadata value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
