using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Shared.Data.Entities;
using Shared.Data.Entities.Constants;
using Shared.Data.Services;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class TransactionsRepository : RepositoryBase<TransactionEntity>, ITransactionsRepository
{

    public TransactionsRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        IKafkaProducerService<Command> commandsProducer)
        : base(
            memoryCache,
            mongoDbFactory.GetDatabase(),
            commandsProducer,
            CollectionNames.Transactions)
    {
        
    }

    public async Task<IEnumerable<TransactionEntity>> AccountTransactionHistory(string accountId)
    {
        return await Collection
            .Find(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
            .SortByDescending(t => t.DateTime)
            .ToListAsync();
    }
}