using Contracts.Extensions;
using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.Identity;
using Shared.Data.Services;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class UsersRepository : RepositoryBase<ApplicationUser>, IUsersRepository
{
    
    public UsersRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        IKafkaProducerService<Command> commandsProducer)
        : base(
            memoryCache,
            mongoDbFactory.GetDatabase(), 
            commandsProducer,
            CollectionNames.Service.Users)
    {
        
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return (await GetAsync(u => u.NormalizedEmail == email.NormalizeString())).FirstOrDefault();
    }
}