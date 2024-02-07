using Contracts;
using DistributedBanking.Processing.Data.Repositories;
using DistributedBanking.Processing.Domain.Mapping;
using DistributedBanking.Processing.Domain.Models.Transaction;
using Mapster;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Shared.Data.Entities.Constants;

namespace DistributedBanking.Processing.Domain.Services.Implementation;

public class TransactionService : ITransactionService
{
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly IAccountsRepository _accountsRepository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionsRepository transactionsRepository, 
        IAccountsRepository accountsRepository,
        ILogger<TransactionService> logger)
    {
        _transactionsRepository = transactionsRepository;
        _accountsRepository = accountsRepository;
        _logger = logger;
    }
    
    public async Task<OperationStatusModel> Deposit(OneWayTransactionModel depositTransactionModel)
    {
        try
        {
            var account = await _accountsRepository.GetAsync(new ObjectId(depositTransactionModel.SourceAccountId));
            if (account == null)
            {
                _logger.LogWarning("Requested deposit account '{AccountId}' does not exist", depositTransactionModel.SourceAccountId);
                return OperationStatusModel.Fail("Error occured while trying to deposit your account. Try again later");
            }
            
            if (!AccountValidator.IsAccountValid(account))
            {
                return OperationStatusModel.Fail("The account you are trying to deposit is expired");
            }
            
            account.Balance += depositTransactionModel.Amount;
            await _accountsRepository.UpdateAsync(account);

            var transaction = depositTransactionModel.AdaptToEntity(TransactionType.Deposit);
            await _transactionsRepository.AddAsync(transaction);
            
            return OperationStatusModel.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a deposit for '{SourceAccountId}' account. Try again later", depositTransactionModel.SourceAccountId);
            throw;
        }
    }
    
    public async Task<OperationStatusModel> Withdraw(OneWaySecuredTransactionModel withdrawTransactionModel)
    {
        try
        {
            var account = await _accountsRepository.GetAsync(new ObjectId(withdrawTransactionModel.SourceAccountId));
            if (account == null)
            {
                _logger.LogWarning("Requested withdrawal account '{AccountId}' does not exist", withdrawTransactionModel.SourceAccountId);
                return OperationStatusModel.Fail("Error occured while trying to withdraw from your account. Try again later");
            }
            
            if (!AccountValidator.IsAccountValid(account, withdrawTransactionModel.SecurityCode))
            {
                return OperationStatusModel.Fail("Provided account information is not valid. Account is expired or entered " +
                                                 "security code is not correct");
            }
            
            if (account.Balance < withdrawTransactionModel.Amount)
            {
                return OperationStatusModel.Fail("Insufficient funds. " +
                                                 "The transaction cannot be completed due to a lack of available funds in the account");
            }
            
            account.Balance -= withdrawTransactionModel.Amount;
            await _accountsRepository.UpdateAsync(account);

            var transaction = withdrawTransactionModel.AdaptToEntity(TransactionType.Withdrawal);
            await _transactionsRepository.AddAsync(transaction);
            
            return OperationStatusModel.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a withdrawal from '{SourceAccountId}' account. Try again later", withdrawTransactionModel.SourceAccountId);
            throw;
        }
    }
    
    public async Task<OperationStatusModel> Transfer(TwoWayTransactionModel transferTransactionModel)
    {
        try
        {
            var destinationAccount = await _accountsRepository.GetAsync(new ObjectId(transferTransactionModel.DestinationAccountId));
            var sourceAccount = await _accountsRepository.GetAsync(new ObjectId(transferTransactionModel.SourceAccountId));
            if (destinationAccount == null || sourceAccount == null)
            {
                _logger.LogWarning("One of the requested transfer accounts '{SourceAccountId}' or '{DestinationAccountId}' does not exist",
                    transferTransactionModel.SourceAccountId, transferTransactionModel.DestinationAccountId);
                
                return OperationStatusModel.Fail("Error occured while trying to make requested transfer from your account. Try again later");
            }
            
            if (!AccountValidator.IsAccountValid(sourceAccount, transferTransactionModel.SourceAccountSecurityCode))
            {
                return OperationStatusModel.Fail("Your account information is not valid. Account is expired or entered " +
                                                   "security code is not correct");
            }
            
            if (!AccountValidator.IsAccountValid(destinationAccount))
            {
                return OperationStatusModel.Fail("Destination account information is not valid. Account is probably expired");
            }
            
            if (sourceAccount.Balance < transferTransactionModel.Amount)
            {
                return OperationStatusModel.Fail("Insufficient funds. " +
                                                   "The transaction cannot be completed due to a lack of available funds in the account");
            }
            
            sourceAccount.Balance -= transferTransactionModel.Amount;
            destinationAccount.Balance += transferTransactionModel.Amount;
            
            await _accountsRepository.UpdateAsync(sourceAccount);
            await _accountsRepository.UpdateAsync(destinationAccount);

            var transaction = transferTransactionModel.AdaptToEntity(TransactionType.Transfer);
            await _transactionsRepository.AddAsync(transaction);
            
            return OperationStatusModel.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a transfer from '{SourceAccountId}' account to '{DestinationAccountId}' account. Try again later", 
                transferTransactionModel.SourceAccountId, transferTransactionModel.DestinationAccountId);
            throw;
        }
    }

    public async Task<decimal> GetBalance(string accountId)
    {
        var account = await _accountsRepository.GetAsync(new ObjectId(accountId));
        
        return account?.Balance ?? 0;
    }

    public async Task<IEnumerable<TransactionResponseModel>> GetAccountTransactionHistory(string accountId)
    {
        var transactions = await _transactionsRepository.AccountTransactionHistory(accountId);

        return transactions.Adapt<TransactionResponseModel[]>();
    }
}