using DistributedBanking.Processing.Data.Repositories.Base;
using Shared.Data.Entities;

namespace DistributedBanking.Processing.Data.Repositories;

public interface ITransactionsRepository : IRepositoryBase<TransactionEntity>
{
    Task<IEnumerable<TransactionEntity>> AccountTransactionHistory(string accountId);
}