using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Account;
using Shared.Data.Entities;

namespace DistributedBanking.Processing.Domain.Services;

public interface IAccountService
{
    Task<OperationResult<AccountOwnedResponseModel>> CreateAsync(string customerId, AccountCreationModel accountModel);
    Task<AccountOwnedResponseModel> GetAsync(string id);
    Task<IEnumerable<AccountOwnedResponseModel>> GetAsync();
    Task<IEnumerable<AccountResponseModel>> GetCustomerAccountsAsync(string customerId);
    Task<bool> BelongsTo(string accountId, string customerId);
    Task UpdateAsync(AccountEntity model);
    Task<OperationResult> DeleteAsync(string id);
}