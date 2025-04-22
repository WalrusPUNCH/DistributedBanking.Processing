using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.Identity;
using Shared.Data.Services;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class RolesRepository : RepositoryBase<ApplicationRole>, IRolesRepository
{
    public RolesRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        IKafkaProducerService<Command> commandsProducer)
        : base(
            memoryCache,
            mongoDbFactory.GetDatabase(), 
            commandsProducer,
            CollectionNames.Service.Roles)
    {
    }
}