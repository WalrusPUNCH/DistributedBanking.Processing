using DistributedBanking.Processing.Data.Repositories.Base;
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
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.Accounts)
    {
        _database = mongoDbFactory.GetDatabase();
    }
}