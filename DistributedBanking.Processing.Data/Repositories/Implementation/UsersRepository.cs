using Contracts.Extensions;
using DistributedBanking.Processing.Data.Repositories.Base;
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
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.Accounts)
    {
        _database = mongoDbFactory.GetDatabase();
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return (await GetAsync(u => u.NormalizedEmail == email.NormalizeString())).FirstOrDefault();
    }
}