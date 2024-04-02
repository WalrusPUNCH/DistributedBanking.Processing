using Contracts.Models;
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
    
    public async Task<OperationResult> Deposit(OneWayTransactionModel depositTransactionModel)
    {
        try
        {
            var account = await _accountsRepository.GetAsync(new ObjectId(depositTransactionModel.SourceAccountId));
            if (account == null)
            {
                _logger.LogWarning("Requested deposit account '{AccountId}' does not exist", depositTransactionModel.SourceAccountId);
                return OperationResult.BadRequest("Specified deposit account doesn't exist");
            }
            
            if (!AccountValidator.IsAccountValid(account))
            {
                return OperationResult.BadRequest("The account you are trying to deposit is expired");
            }
            
            account.Balance += depositTransactionModel.Amount;
            await _accountsRepository.UpdateAsync(account, 10);

            var transaction = depositTransactionModel.AdaptToEntity(TransactionType.Deposit);
            await _transactionsRepository.AddAsync(transaction, 10);
            
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a deposit for '{SourceAccountId}' account. Try again later", depositTransactionModel.SourceAccountId);
            
            return OperationResult.InternalFail("Error occured while trying to make a deposit. Try again later");
        }
    }
    
    public async Task<OperationResult> Withdraw(OneWaySecuredTransactionModel withdrawTransactionModel)
    {
        try
        {
            var account = await _accountsRepository.GetAsync(new ObjectId(withdrawTransactionModel.SourceAccountId));
            if (account == null)
            {
                _logger.LogWarning("Requested withdrawal account '{AccountId}' does not exist", withdrawTransactionModel.SourceAccountId);
                return OperationResult.BadRequest("Specified withdrawal account doesn't exist");
            }
            
            if (!AccountValidator.IsAccountValid(account, withdrawTransactionModel.SecurityCode))
            {
                return OperationResult.BadRequest("Provided account information is not valid. Account is expired or entered " +
                                            "security code is not correct");
            }
            
            if (account.Balance < withdrawTransactionModel.Amount)
            {
                return OperationResult.BadRequest("Insufficient funds. " +
                                            "The transaction cannot be completed due to a lack of available funds in the account");
            }
            
            account.Balance -= withdrawTransactionModel.Amount;
            await _accountsRepository.UpdateAsync(account, 10);

            var transaction = withdrawTransactionModel.AdaptToEntity(TransactionType.Withdrawal);
            await _transactionsRepository.AddAsync(transaction, 10);
            
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a withdrawal from '{SourceAccountId}' account. Try again later", withdrawTransactionModel.SourceAccountId);
            
            return OperationResult.InternalFail("Error occured while trying to make a withdrawal. Try again later");
        }
    }
    
    public async Task<OperationResult> Transfer(TwoWayTransactionModel transferTransactionModel)
    {
        try
        {
            var destinationAccount = await _accountsRepository.GetAsync(new ObjectId(transferTransactionModel.DestinationAccountId));
            var sourceAccount = await _accountsRepository.GetAsync(new ObjectId(transferTransactionModel.SourceAccountId));
            if (destinationAccount == null || sourceAccount == null)
            {
                _logger.LogWarning("One of the specified transfer accounts '{SourceAccountId}' or '{DestinationAccountId}' does not exist",
                    transferTransactionModel.SourceAccountId, transferTransactionModel.DestinationAccountId);
                return OperationResult.BadRequest("One of the specified transfer accounts doesn't exist");
            }
            
            if (!AccountValidator.IsAccountValid(sourceAccount, transferTransactionModel.SourceAccountSecurityCode))
            {
                return OperationResult.BadRequest("Your account information is not valid. Account is expired or entered " +
                                                  "security code is not correct");
            }
            
            if (!AccountValidator.IsAccountValid(destinationAccount))
            {
                return OperationResult.BadRequest("Destination account information is not valid. Account is probably expired");
            }
            
            if (sourceAccount.Balance < transferTransactionModel.Amount)
            {
                return OperationResult.BadRequest("Insufficient funds. " +
                                                  "The transaction cannot be completed due to a lack of available funds in the account");
            }
            
            sourceAccount.Balance -= transferTransactionModel.Amount;
            destinationAccount.Balance += transferTransactionModel.Amount;
            
            await _accountsRepository.UpdateAsync(sourceAccount, 10);
            await _accountsRepository.UpdateAsync(destinationAccount, 10);

            var transaction = transferTransactionModel.AdaptToEntity(TransactionType.Transfer);
            await _transactionsRepository.AddAsync(transaction, 10);
            
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to perform a transfer from '{SourceAccountId}' account to '{DestinationAccountId}' account. Try again later", 
                transferTransactionModel.SourceAccountId, transferTransactionModel.DestinationAccountId);
            return OperationResult.InternalFail("Error occured while trying to make a transfer. Try again later");
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