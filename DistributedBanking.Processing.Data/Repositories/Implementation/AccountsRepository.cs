using DistributedBanking.Processing.Data.Repositories.Base;
using MongoDB.Driver;
using Shared.Data.Entities;
using Shared.Data.Entities.Constants;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class AccountsRepository : RepositoryBase<AccountEntity>, IAccountsRepository
{
    private IMongoDatabase _database;
    
    public AccountsRepository(
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