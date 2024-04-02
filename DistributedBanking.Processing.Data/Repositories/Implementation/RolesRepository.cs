using DistributedBanking.Processing.Data.Repositories.Base;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.Identity;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class RolesRepository : RepositoryBase<ApplicationRole>, IRolesRepository
{
    private IMongoDatabase _database;
    
    public RolesRepository(
        IMemoryCache memoryCache,
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            memoryCache,
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.Service.Roles)
    {
        _database = mongoDbFactory.GetDatabase();
    }
}