using Contracts;
using DistributedBanking.Processing.Domain.Models.Transaction;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Data.Entities.Constants;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Transaction;
using Shared.Kafka.Services;
using Shared.Redis.Models;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Transaction;

public class TransactionsListener : BaseListener<string, TransactionMessage, OperationStatusModel>
{
    private readonly ITransactionService _transactionService;

    public TransactionsListener(
        IKafkaConsumerService<string, TransactionMessage> transactionsConsumer,
        ITransactionService transactionService,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<TransactionsListener> logger) : base(transactionsConsumer, redisSubscriber, redisProvider, logger)
    {
        _transactionService = transactionService;
    }

    protected override bool FilterMessage(MessageWrapper<TransactionMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) 
            && !string.IsNullOrWhiteSpace(messageWrapper.Message.SourceAccountId) 
            && messageWrapper.Message.Amount != 0;
    }

    protected override async Task<ListenerResponse<OperationStatusModel>> ProcessMessage(
        MessageWrapper<TransactionMessage> messageWrapper,
        CancellationToken token)
    {
        token.Register(() => Logger.LogInformation("Transaction operation for source account: {SourceAccountId}, " +
                                                   "destination account: {DestinationAccountId} has been canceled", 
            messageWrapper.Message.SourceAccountId, messageWrapper.Message.DestinationAccountId));

        switch (messageWrapper.Message.Type)
        {
            case TransactionType.Deposit:
            {
                var transactionModel = messageWrapper.Adapt<OneWayTransactionModel>();
                var depositResult = await _transactionService.Deposit(transactionModel /*, token*/);

                return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, depositResult);
            }
            case TransactionType.Withdrawal:
            {
                var transactionModel = messageWrapper.Adapt<OneWaySecuredTransactionModel>();
                var withdrawalResult = await _transactionService.Withdraw(transactionModel /*, token*/);

                return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, withdrawalResult);
            }
            case TransactionType.Transfer:
            {
                var transactionModel = messageWrapper.Adapt<TwoWayTransactionModel>();
                var transferResult = await _transactionService.Transfer(transactionModel /*, token*/);

                return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, transferResult);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(messageWrapper.Message.Type), messageWrapper.Message.Type,
                    "Unknown transaction type has been received");
        }
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<TransactionMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to proceed transaction. Source account: {SourceAccountId}, " + 
                                   "destination account: {DestinationAccountId}. Retry in {Delay} seconds", 
            messageWrapper.Message.SourceAccountId, messageWrapper.Message.DestinationAccountId, delay);
    }
    
    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.TransactionsCreationChannel;
    }
}