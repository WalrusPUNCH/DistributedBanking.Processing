using Contracts;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Account;
using Shared.Kafka.Services;
using Shared.Redis.Models;
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
        MessageWrapper<AccountDeletionMessage> messageWrapper, 
        CancellationToken token)
    {
        token.Register(() => Logger.LogInformation("Account deletion operation for account '{AccountId}' has been canceled",
            messageWrapper.Message.AccountId));

        var deletionResult = await _accountService.DeleteAsync(messageWrapper.Message.AccountId/*, token*/);
        
        return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, deletionResult);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<AccountDeletionMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to delete '{AccountId}' account. Retry in {Delay}",
            messageWrapper.Message.AccountId, delay);
    }

    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.AccountDeletionChannel;
    }
}