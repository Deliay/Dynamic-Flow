
using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace DynamicFlow.Infrastruction.Util;

public static class MongoHelper
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursor<T> asyncCursor, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await asyncCursor.MoveNextAsync(cancellationToken))
        {
            foreach (var current in asyncCursor.Current)
            {
                yield return current;
            }
        }
    }
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IFindFluent<T, T> findFluent, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var asyncCursor = await findFluent.ToCursorAsync(cancellationToken);

        await foreach (var item in asyncCursor.ToAsyncEnumerable(cancellationToken))
        {
            yield return item;
        }
    }

    public static IFindFluent<T, T> Select<T>(this IMongoCollection<T> collection, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> builder)
    {
        return collection.Find(builder(Builders<T>.Filter));
    }
}