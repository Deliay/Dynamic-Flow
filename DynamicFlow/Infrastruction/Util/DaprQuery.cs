using System.Text.Json;
using Dapr.Client;

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

    public static ValueTask<T> BeginDaprLock<T>(this DaprClient client, string store, string id, Func<ValueTask<T>> op, CancellationToken cancellationToken = default)
        => BeginDaprLock<T>(client, store, id, TimeSpan.FromSeconds(3), op, cancellationToken);

    private static async ValueTask<bool> Wrap(Func<ValueTask> op) {
        await op();
        return true;
    }

    public static async ValueTask BeginDaprLock(this DaprClient client, string store, string id, Func<ValueTask> op, CancellationToken cancellationToken = default)
    {
        var _ = await BeginDaprLock(client, store, id, TimeSpan.FromSeconds(3), () => Wrap(op), cancellationToken);
    }

    public static async ValueTask<T> BeginDaprLock<T>(this DaprClient client, string store, string id, TimeSpan expire, Func<ValueTask<T>> op, CancellationToken cancellationToken = default)
    {
        await using var _lock = await client.Lock(store, id, Guid.NewGuid().ToString(), (int)expire.TotalSeconds, cancellationToken);

        if (!_lock.Success) throw new InvalidOperationException();

        try
        {
            return await op();
        }
        finally
        {
            await client.Unlock(store, id, _lock.LockOwner, cancellationToken);
        }
    }
}