using System.Text.Json;

namespace DynamicFlow.Infrastruction.Util;

public static class DaprQuery
{
    private record struct Filter<T>(T filter);

    private static Filter<T> TransFilter<T>(T search)
    {
        return new(search);
    }

    public static string EQ<T>(string property, T value)
    {
        return JsonSerializer.Serialize(TransFilter(new { EQ = new Dictionary<string, T>()
        {
            { property, value },
        } }));
    }

    public static string IN<T>(string property, List<T> items)
    {
        return JsonSerializer.Serialize(TransFilter(new { IN = new Dictionary<string, List<T>>()
        {
            { property, items },
        } }));
    }

    public static string AND(List<object> conditions)
    {
        return JsonSerializer.Serialize(TransFilter(new { AND = conditions }));
    }

    public static string OR(List<object> conditions)
    {
        return JsonSerializer.Serialize(TransFilter(new { OR = conditions }));
    }
}