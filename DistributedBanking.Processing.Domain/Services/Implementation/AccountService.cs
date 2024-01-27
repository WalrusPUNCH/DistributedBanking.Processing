using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Domain.Models.Account;
using Mapster;
using MongoDB.Bson;
using Shared.Data.Entities;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class AccountService : IAccountService
{
    private readonly IAccountsRepository _accountsRepository;
    private readonly ICustomersRepository _customersRepository;

    public AccountService(
        IAccountsRepository accountsRepository, 
        ICustomersRepository customersRepository)
    {
        _accountsRepository = accountsRepository;
        _customersRepository = customersRepository;
    }
    
    public async Task<AccountOwnedResponseModel> CreateAsync(string customerId, AccountCreationModel accountCreationModel)
    {
        var account = GenerateNewAccount(customerId, accountCreationModel);
        var accountEntity = account.Adapt<AccountEntity>();
        await _accountsRepository.AddAsync(accountEntity);
        
        var customerEntity = await _customersRepository.GetAsync(new ObjectId(customerId));
        customerEntity.Accounts.Add(accountEntity.Id.ToString());

        await _customersRepository.UpdateAsync(customerEntity);
        
        return accountEntity.Adapt<AccountOwnedResponseModel>();
    }

    private static AccountModel GenerateNewAccount(string customerId, AccountCreationModel accountModel)
    {
        return new AccountModel
        {
            Name = accountModel.Name,
            Type = accountModel.Type,
            Balance = 0,
            ExpirationDate = Generator.GenerateExpirationDate(),
            SecurityCode = Generator.GenerateSecurityCode(),
            Owner = customerId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<AccountOwnedResponseModel> GetAsync(string id)
    {
        var account = await _accountsRepository.GetAsync(new ObjectId(id));

        return account.Adapt<AccountOwnedResponseModel>();
    }

    public async Task<IEnumerable<AccountResponseModel>> GetCustomerAccountsAsync(string customerId)
    {
        var accounts = await _accountsRepository.GetAsync(x => x.Owner == customerId);
        
        return accounts.Adapt<AccountResponseModel[]>();
    }

    public async Task<bool> BelongsTo(string accountId, string customerId)
    {
        var account = await _accountsRepository.GetAsync(
            a => a.Id == new ObjectId(accountId) && a.Owner != null && a.Owner == customerId);

        return account.Any();
    }

    public async Task<IEnumerable<AccountOwnedResponseModel>> GetAsync()
    {
        var accounts = await _accountsRepository.GetAsync();
        
        return accounts.Adapt<AccountOwnedResponseModel[]>();
    }

    public async Task UpdateAsync(AccountEntity model)
    {
        await _accountsRepository.UpdateAsync(model);
    }

    public async Task DeleteAsync(string id)
    {
        var accountEntity = await _accountsRepository.GetAsync(new ObjectId(id));
        if (string.IsNullOrWhiteSpace(accountEntity.Owner))
        {
            return;
        }
        
        var customerEntity = await _customersRepository.GetAsync(new ObjectId(accountEntity.Owner));
        customerEntity.Accounts.Remove(accountEntity.Id.ToString());
        await _customersRepository.UpdateAsync(customerEntity);
        accountEntity.Owner = null;
        await UpdateAsync(accountEntity);
    }
}