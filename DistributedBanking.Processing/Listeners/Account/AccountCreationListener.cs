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

public class AccountCreationListener : BaseListener<string, AccountCreationMessage, OperationStatusModel<AccountOwnedResponseModel>>
{
    private readonly IAccountService _accountService;

    public AccountCreationListener(
        IKafkaConsumerService<string, AccountCreationMessage> accountCreationConsumer,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        IAccountService accountService,
        ILogger<AccountCreationListener> logger) : base(accountCreationConsumer, redisSubscriber, redisProvider, logger)
    {
        _accountService = accountService;
    }
    
    protected override bool FilterMessage(MessageWrapper<AccountCreationMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.CustomerId);
    }
    
    protected override async Task<ListenerResponse<OperationStatusModel<AccountOwnedResponseModel>>> ProcessMessage(
        MessageWrapper<AccountCreationMessage> messageWrapper)
    {
        var accountCreationModel = messageWrapper.Adapt<AccountCreationModel>();
        var accountCreationResult = await _accountService.CreateAsync(messageWrapper.Message.CustomerId, accountCreationModel);

        return new ListenerResponse<OperationStatusModel<AccountOwnedResponseModel>>(
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