using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Shared.Data.Entities;
using Shared.Data.Entities.Constants;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class TransactionsRepository : RepositoryBase<TransactionEntity>, ITransactionsRepository
{
    private IMongoDatabase _database;
    
    public TransactionsRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            memoryCache,
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.Transactions)
    {
        _database = mongoDbFactory.GetDatabase();
    }

    public async Task<IEnumerable<TransactionEntity>> AccountTransactionHistory(string accountId)
    {
        return await Collection
            .Find(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
            .SortByDescending(t => t.DateTime)
            .ToListAsync();
    }
}