using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Account;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages.Account;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Account;

public class AccountCreationListener : BaseListener<string, AccountCreationMessage, OperationResult<AccountOwnedResponseModel>>
{
    private readonly IAccountService _accountService;

    public AccountCreationListener(
        IKafkaConsumerService<string, AccountCreationMessage> accountCreationConsumer,
        IRedisSubscriber redisSubscriber,
        IAccountService accountService,
        ILogger<AccountCreationListener> logger) : base(accountCreationConsumer, redisSubscriber, logger)
    {
        _accountService = accountService;
    }
    
    protected override bool FilterMessage(MessageWrapper<AccountCreationMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.CustomerId);
    }
    
    protected override async Task<ListenerResponse<OperationResult<AccountOwnedResponseModel>>> ProcessMessage(
        MessageWrapper<AccountCreationMessage> messageWrapper)
    {
        var accountCreationModel = messageWrapper.Message.Adapt<AccountCreationModel>();
        var accountCreationResult = await _accountService.CreateAsync(messageWrapper.Message.CustomerId, accountCreationModel);

        return new ListenerResponse<OperationResult<AccountOwnedResponseModel>>(
            MessageOffset: messageWrapper.Offset, 
            Response: accountCreationResult, 
            ResponseChannelPattern: messageWrapper.Message.ResponseChannelPattern);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<AccountCreationMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to create account for customer wit an ID '{CustomerId}'. Retry in {Delay}",
            messageWrapper.Message.CustomerId, delay);
    }
}