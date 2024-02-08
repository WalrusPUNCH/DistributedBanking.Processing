using DistributedBanking.Processing.Data.Repositories.Base;
using MongoDB.Driver;
using Shared.Data.Entities.Constants;
using Shared.Data.Entities.EndUsers;
using Shared.Data.Services;
using TransactionalClock.Integration;

namespace DistributedBanking.Processing.Data.Repositories.Implementation;

public class WorkersRepository : RepositoryBase<WorkerEntity>, IWorkersRepository
{
    private IMongoDatabase _database;
    
    public WorkersRepository(
        IMongoDbFactory mongoDbFactory,
        ITransactionalClockClient transactionalClockClient) 
        : base(
            transactionalClockClient, 
            mongoDbFactory.GetDatabase(), 
            CollectionNames.EndUsers)
    {
        _database = mongoDbFactory.GetDatabase();
    }
}