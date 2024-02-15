using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Data.Entities;
using System.Linq.Expressions;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Base;

public class RepositoryBase<T> : IRepositoryBase<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection;
    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;
   // private readonly MongoCollectionSettings _mongoCollectionSettings = new() { GuidRepresentation = GuidRepresentation.Standard };
    
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly ITransactionalClockClient _transactionalClockClient;
    
    protected RepositoryBase(
        ITransactionalClockClient transactionalClockClient,
        IMongoDatabase database,
        string collectionName)
    {
        if (!database.ListCollectionNames().ToList().Contains(collectionName))
        {
            database.CreateCollection(collectionName);
        }
        
        Collection = database.GetCollection<T>(collectionName/*, _mongoCollectionSettings*/);
        
        _databaseName = database.DatabaseNamespace.DatabaseName;
        _collectionName = collectionName;
        
        _transactionalClockClient = transactionalClockClient;
    }

    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        return await Collection.Find(FilterDefinition<T>.Empty).ToListAsync();
    }

    public virtual async Task<T?> GetAsync(ObjectId id)
    {
        var filter = _filterBuilder.Eq(e => e.Id, id);
        return await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter)
    {
        return await Collection.Find(filter ?? FilterDefinition<T>.Empty).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var transactionalClockResponse = await _transactionalClockClient.Create(
            database: _databaseName,
            collection: _collectionName,
            entity);

        entity.Id = transactionalClockResponse.Id;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
        
        await _transactionalClockClient.Update(
            id: entity.Id.ToString(),
            database: _databaseName,
            collection: _collectionName,
            createdAt: DateTime.UtcNow.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ"),
            entity);
    }

    public virtual async Task RemoveAsync(ObjectId id)
    {
        await _transactionalClockClient.Delete(
            id: id.ToString(),
            database: _databaseName,
            collection: _collectionName);
    }
}