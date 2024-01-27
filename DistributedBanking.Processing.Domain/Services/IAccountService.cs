using DistributedBanking.Processing.Domain.Models.Account;
using Shared.Data.Entities;

namespace DistributedBanking.Processing.Domain.Services;

public interface IAccountService
{
    Task<AccountOwnedResponseModel> CreateAsync(string customerId, AccountCreationModel accountModel);
    Task<AccountOwnedResponseModel> GetAsync(string id);
    Task<IEnumerable<AccountResponseModel>> GetCustomerAccountsAsync(string customerId);
    Task<bool> BelongsTo(string accountId, string customerId);
    Task<IEnumerable<AccountOwnedResponseModel>> GetAsync();
    Task UpdateAsync(AccountEntity model);
    Task DeleteAsync(string id);
}