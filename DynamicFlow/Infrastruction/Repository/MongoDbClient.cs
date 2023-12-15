
using System.Security;
using DynamicFlow.Application.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Authentication;

namespace DynamicFlow.Infrastruction.Repository;

public interface IMongoRespository
{
    public IMongoCollection<LabeledTreeObject> Trees { get; }
    public IMongoCollection<LabeledTaskObject> Tasks { get; }
}

public class MongoDbClient() : MongoClient(new MongoClientSettings()
{
    Server = new(MongoHost),
    Credential = MongoCredential.CreatePlainCredential(MongoDatabase, null, MongoPassword),
}), IMongoRespository
{
    private readonly static SecureString MongoPassword = (Environment.GetEnvironmentVariable("mongo-password")
            ?? throw new InvalidDataException("Missing mongo-host environment"))
        .Aggregate(new SecureString(), (ss, ch) => { ss.AppendChar(ch); return ss; });

    private readonly static string MongoHost = Environment.GetEnvironmentVariable("mongo-host")
        ?? throw new InvalidDataException("Missing mongo-host environment");
    private readonly static string MongoDatabase = Environment.GetEnvironmentVariable("mongo-db")
        ?? throw new InvalidDataException("Missing mongo-host environment");

    public IMongoCollection<T> CollectionOf<T>(string collection) => GetDatabase(MongoDatabase).GetCollection<T>(collection);

    public IMongoCollection<LabeledTreeObject> Trees => CollectionOf<LabeledTreeObject>("trees");
    public IMongoCollection<LabeledTaskObject> Tasks => CollectionOf<LabeledTaskObject>("tasks");
}