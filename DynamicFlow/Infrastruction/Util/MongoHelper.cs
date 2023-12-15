
using System.Runtime.CompilerServices;
using DynamicFlow.Application.Repository;
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
    public static UpdateDefinitionBuilder<T> Update<T>(this IMongoCollection<T> collection)
    {
        return Builders<T>.Update;
    }
    public static FilterDefinitionBuilder<T> Filter<T>(this IMongoCollection<T> collection)
    {
        return Builders<T>.Filter;
    }
    public static FilterDefinition<LabeledTreeObject> Filter(this IMongoCollection<LabeledTreeObject> collection, string id)
    {
        return Builders<LabeledTreeObject>.Filter.Eq(tree => tree.Id, id);
    }
    public static FilterDefinition<LabeledTaskObject> Filter(this IMongoCollection<LabeledTaskObject> collection, string id)
    {
        return Builders<LabeledTaskObject>.Filter.Eq(tree => tree.Id, id);
    }

    public static IFindFluent<LabeledTreeObject, LabeledTreeObject> SelectId(this IMongoCollection<LabeledTreeObject> collection, string id)
    {
        return collection.Select(where => where.Eq(tree => tree.Id, id));
    }

    public static IFindFluent<LabeledTaskObject, LabeledTaskObject> SelectId(this IMongoCollection<LabeledTaskObject> collection, string id)
    {
        return collection.Select(where => where.Eq(tree => tree.Id, id));
    }
}