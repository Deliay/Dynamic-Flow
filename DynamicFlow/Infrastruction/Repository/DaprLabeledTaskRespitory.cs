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
    public IMongoCollection<LabeledTaskObject> Collection => Db.Tasks;
    private const string DaprStore = "flow_task_" + nameof(DaprLabeledTaskRespitory);

    public async ValueTask Save(LabeledTaskObject task, CancellationToken cancellationToken)
    {
        if (await Db.Tasks.Select(where => where.Eq(task => task.Id, task.Id)).AnyAsync(cancellationToken))
            throw new InvalidDataException("Task.Id duplicated");

        await Db.Tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
    }

    public async ValueTask<LabeledTaskObject> Get(string id, CancellationToken cancellationToken)
    {
        return await Db.Tasks.Select(where => where.Eq(task => task.Id, id)).FirstOrDefaultAsync(cancellationToken);
    }

    public IAsyncEnumerable<LabeledTaskObject> Search(LabelMetadata metadata, List<string> values, CancellationToken cancellationToken)
    {
        return Search(new(){{ metadata, values }}, cancellationToken);
    }

    public IAsyncEnumerable<LabeledTaskObject> Search(Dictionary<LabelMetadata, List<string>> conditions, CancellationToken cancellationToken)
    {
        var querys = conditions.SelectMany(pair => pair.Value.Select(value => Builders<LabeledTaskObject>.Filter
            .ElemMatch(task => task.Labels, label => label.Metadata == pair.Key && label.Value == value)));

        var query = Builders<LabeledTaskObject>.Filter.Or(querys);

        return Db.Tasks.Find(query).ToAsyncEnumerable(cancellationToken);
    }
}