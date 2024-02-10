using Contracts;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages.Account;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Account;

public class AccountDeletionListener : BaseListener<string, AccountDeletionMessage, OperationStatusModel>
{
    private readonly IAccountService _accountService;

    public AccountDeletionListener(
        IKafkaConsumerService<string, AccountDeletionMessage> accountDeletionConsumer,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        IAccountService accountService,
        ILogger<AccountDeletionListener> logger) : base(accountDeletionConsumer, redisSubscriber, redisProvider, logger)
    {
        _accountService = accountService;
    }

    protected override bool FilterMessage(MessageWrapper<AccountDeletionMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.AccountId);
    }

    protected override async Task<ListenerResponse<OperationStatusModel>> ProcessMessage(
        MessageWrapper<AccountDeletionMessage> messageWrapper)
    {
        var deletionResult = await _accountService.DeleteAsync(messageWrapper.Message.AccountId);
        return new ListenerResponse<OperationStatusModel>(
            MessageOffset: messageWrapper.Offset, 
            Response: deletionResult,
            ResponseChannelPattern: messageWrapper.Message.ResponseChannelPattern);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<AccountDeletionMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to delete '{AccountId}' account. Retry in {Delay}",
            messageWrapper.Message.AccountId, delay);
    }
}