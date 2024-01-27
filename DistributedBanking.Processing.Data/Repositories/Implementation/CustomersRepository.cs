using DistributedBanking.Processing.Data.Repositories.Base;
using MongoDB.Driver;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.EndUsers;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class CustomersRepository : RepositoryBase<CustomerEntity>, ICustomersRepository
{
    private IMongoDatabase _database;
    
    public CustomersRepository(
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