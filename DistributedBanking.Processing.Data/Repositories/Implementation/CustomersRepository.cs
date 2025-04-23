using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.EndUsers;
using Shared.Data.Services;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class CustomersRepository : RepositoryBase<CustomerEntity>, ICustomersRepository
{
    
    public CustomersRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        IKafkaProducerService<Command> commandsProducer)
        : base(
            memoryCache,
            mongoDbFactory.GetDatabase(), 
            commandsProducer,
            CollectionNames.EndUsers)
    {
    }
}