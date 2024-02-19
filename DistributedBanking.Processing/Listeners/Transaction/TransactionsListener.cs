using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Transaction;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Data.Entities.Constants;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages.Transaction;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Transaction;

public class TransactionsListener : BaseListener<string, TransactionMessage, OperationResult>
{
    private readonly ITransactionService _transactionService;

    public TransactionsListener(
        IKafkaConsumerService<string, TransactionMessage> transactionsConsumer,
        ITransactionService transactionService,
        IRedisSubscriber redisSubscriber,
        ILogger<TransactionsListener> logger) : base(transactionsConsumer, redisSubscriber, logger)
    {
        _transactionService = transactionService;
    }

    protected override bool FilterMessage(MessageWrapper<TransactionMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) 
            && !string.IsNullOrWhiteSpace(messageWrapper.Message.SourceAccountId) 
            && messageWrapper.Message.Amount != 0;
    }

    protected override async Task<ListenerResponse<OperationResult>> ProcessMessage(
        MessageWrapper<TransactionMessage> messageWrapper)
    {
        switch (messageWrapper.Message.Type)
        {
            case TransactionType.Deposit:
            {
                var transactionModel = messageWrapper.Message.Adapt<OneWayTransactionModel>();
                var depositResult = await _transactionService.Deposit(transactionModel);

                return new ListenerResponse<OperationResult>(messageWrapper.Offset, depositResult, messageWrapper.Message.ResponseChannelPattern);
            }
            case TransactionType.Withdrawal:
            {
                var transactionModel = messageWrapper.Message.Adapt<OneWaySecuredTransactionModel>();
                var withdrawalResult = await _transactionService.Withdraw(transactionModel);

                return new ListenerResponse<OperationResult>(messageWrapper.Offset, withdrawalResult, messageWrapper.Message.ResponseChannelPattern);
            }
            case TransactionType.Transfer:
            {
                var transactionModel = messageWrapper.Message.Adapt<TwoWayTransactionModel>();
                var transferResult = await _transactionService.Transfer(transactionModel);

                return new ListenerResponse<OperationResult>(messageWrapper.Offset, transferResult, messageWrapper.Message.ResponseChannelPattern);
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
}