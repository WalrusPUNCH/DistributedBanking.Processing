using Contracts.Extensions;
using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.Identity;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class UsersRepository : RepositoryBase<ApplicationUser>, IUsersRepository
{
    private IMongoDatabase _database;
    
    public UsersRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            memoryCache,
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.Service.Users)
    {
        _database = mongoDbFactory.GetDatabase();
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return (await GetAsync(u => u.NormalizedEmail == email.NormalizeString())).FirstOrDefault();
    }
}