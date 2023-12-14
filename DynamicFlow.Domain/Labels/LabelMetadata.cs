using System.ComponentModel;

namespace DynamicFlow.Domain.Labels
{
    public readonly struct LabelMetadata(string @namespace, string name, int allowCount)
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
    }
}
