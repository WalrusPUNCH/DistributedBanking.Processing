using Confluent.Kafka;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Data.Entities;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using System.Linq.Expressions;

namespace DistributedBanking.Processing.Data.Repositories.Base;

public class RepositoryBase<T> : IRepositoryBase<T> where T : BaseEntity
{
    private readonly IMemoryCache _memoryCache;
    protected readonly IMongoCollection<T> Collection;
    private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;
    
    private readonly string _collectionName;
    private readonly IKafkaProducerService<Command> _commandsProducer; 
    
    protected RepositoryBase(
        IMemoryCache memoryCache,
        IMongoDatabase database,
        IKafkaProducerService<Command> commandsProducer,
        string collectionName)
    {
        if (!database.ListCollectionNames().ToList().Contains(collectionName))
        {
            database.CreateCollection(collectionName);
        }
        
        Collection = database.GetCollection<T>(collectionName);
        
        _memoryCache = memoryCache;
        _collectionName = collectionName;

        _commandsProducer = commandsProducer;
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

    public virtual async Task AddAsync(T entity, int priority = 50)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            _memoryCache.Set(entity.Id, entity, TimeSpan.FromSeconds(2));

            var command = new Command(
                _collectionName,
                entity.Id.ToString(),
                CommandType.Create,
                DateTime.UtcNow, 
                entity,
                entity.GetType(),
                priority);
            
            var messageDelivery = await _commandsProducer.ProduceAsync(command);
            if (messageDelivery.Status != PersistenceStatus.Persisted)
            {
                throw new KafkaException(new Error(ErrorCode.Unknown, "Message delivery failed"));
            }
        }
        catch (Exception)
        {
            _memoryCache.Remove(entity.Id);
            throw;
        }
    }

    public virtual async Task UpdateAsync(T entity, int priority = 50)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            _memoryCache.Set(entity.Id, entity, TimeSpan.FromSeconds(2));
        
            var command = new Command(
                _collectionName,
                entity.Id.ToString(),
                CommandType.Update,
                DateTime.UtcNow, 
                entity,
                entity.GetType(),
                priority);
            
            var messageDelivery = await _commandsProducer.ProduceAsync(command);
            if (messageDelivery.Status != PersistenceStatus.Persisted)
            {
                throw new KafkaException(new Error(ErrorCode.Unknown, "Message delivery failed"));
            }
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

        var command = new Command(
            _collectionName,
            id.ToString(),
            CommandType.Delete,
            DateTime.UtcNow, 
            null,
            null);
            
        var messageDelivery = await _commandsProducer.ProduceAsync(command);
        if (messageDelivery.Status != PersistenceStatus.Persisted)
        {
            throw new KafkaException(new Error(ErrorCode.Unknown, "Message delivery failed"));
        }
    }
}