using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapr.Client;
using DynamicFlow.Application.Repository;
using DynamicFlow.Domain.Labels;
using DynamicFlow.Infrastruction.Util;
using MongoDB.Driver;

namespace DynamicFlow.Infrastruction.Repository;

public class DaprLabeledTaskRespitory(IMongoRespository Db) : ILabeledTaskRepository
{
    private const string DaprStore = "flow_task_" + nameof(DaprLabeledTaskRespitory);

    public async ValueTask Save(LabeledTaskObject task, CancellationToken cancellationToken)
    {
        if (await Db.Tasks._(where => where.Eq(task => task.Id, task.Id)).AnyAsync(cancellationToken))
            throw new InvalidDataException("Task.Id duplicated");

        await Db.Tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
    }

    public async ValueTask<LabeledTaskObject> Get(string id, CancellationToken cancellationToken)
    {
        return await Db.Tasks._(where => where.Eq(task => task.Id, id)).FirstOrDefaultAsync(cancellationToken);
    }

    public IAsyncEnumerable<LabeledTaskObject> Search(LabelMetadata metadata, List<string> values, CancellationToken cancellationToken)
    {
        var querys = values.Select(value => Builders<LabeledTaskObject>.Filter
            .ElemMatch(task => task.Labels, label => label.Metadata == metadata && label.Value == value));

        var query = Builders<LabeledTaskObject>.Filter.Or(querys);

        return Db.Tasks.Find(query).ToAsyncEnumerable(cancellationToken);
    }
}