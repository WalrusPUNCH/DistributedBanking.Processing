using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Data.Entities;
using System.Linq.Expressions;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Base;

public class RepositoryBase<T> : IRepositoryBase<T> where T : BaseEntity
{
    private readonly IMemoryCache _memoryCache;
    protected readonly IMongoCollection<T> Collection;
    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;
    
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly ITransactionalClockClient _transactionalClockClient;
    
    protected RepositoryBase(
        IMemoryCache memoryCache,
        ITransactionalClockClient transactionalClockClient,
        IMongoDatabase database,
        string collectionName)
    {
        if (!database.ListCollectionNames().ToList().Contains(collectionName))
        {
            database.CreateCollection(collectionName);
        }
        
        Collection = database.GetCollection<T>(collectionName);
        
        _databaseName = database.DatabaseNamespace.DatabaseName;
        _memoryCache = memoryCache;
        _collectionName = collectionName;
        
        _transactionalClockClient = transactionalClockClient;
    }

    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        var baseEntities = await Collection.Find(FilterDefinition<T>.Empty).ToListAsync();
        
        var cachedEntities = baseEntities.Select(entity =>
                _memoryCache.TryGetValue<T>(entity.Id, out var value)
                    ? value!
                    : entity)
            .ToList();

        return cachedEntities;
    }

    public virtual async Task<T?> GetAsync(ObjectId id)
    {
        var filter = _filterBuilder.Eq(e => e.Id, id);
        _memoryCache.TryGetValue<T>(id, out var value);
        
        return value ?? await Collection.Find(filter).FirstOrDefaultAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? filter)
    {
        var baseEntities = await Collection.Find(filter ?? FilterDefinition<T>.Empty).ToListAsync();

        var cachedEntities = baseEntities.Select(entity =>
                _memoryCache.TryGetValue<T>(entity.Id, out var value)
                    ? value!
                    : entity)
            .ToList();

        return cachedEntities;
    }

    public virtual async Task AddAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            _memoryCache.Set(entity.Id, entity, TimeSpan.FromSeconds(2));

            var transactionalClockResponse = await _transactionalClockClient.Create(
                database: _databaseName,
                collection: _collectionName,
                entity);

            entity.Id = transactionalClockResponse.Id;
        }
        catch (Exception)
        {
            _memoryCache.Remove(entity.Id);
            throw;
        }
    }

    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            _memoryCache.Set(entity.Id, entity, TimeSpan.FromSeconds(2));
        
            await _transactionalClockClient.Update(
                id: entity.Id.ToString(),
                database: _databaseName,
                collection: _collectionName,
                createdAt: DateTime.UtcNow.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ"),
                entity);
        }
        catch (Exception)
        {
            _memoryCache.Remove(entity.Id);
            throw;
        }
    }

    public virtual async Task RemoveAsync(ObjectId id)
    {
        _memoryCache.Remove(id);

        await _transactionalClockClient.Delete(
            id: id.ToString(),
            database: _databaseName,
            collection: _collectionName);
    }
}