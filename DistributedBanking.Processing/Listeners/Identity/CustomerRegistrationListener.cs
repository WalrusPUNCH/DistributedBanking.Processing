using DistributedBanking.Processing.Domain.Models.Identity;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Data.Entities.Constants;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Identity.Registration;
using Shared.Kafka.Services;
using Shared.Redis.Models;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class CustomerRegistrationListener : BaseListener<string, UserRegistrationMessage, IdentityOperationResult>
{
    private readonly IIdentityService _identityService;

    public CustomerRegistrationListener(
        IKafkaConsumerService<string, UserRegistrationMessage> userRegistrationConsumer,
        IIdentityService identityService,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<CustomerRegistrationListener> logger) : base(userRegistrationConsumer, redisSubscriber, redisProvider, logger)
    {
        _identityService = identityService;
    }

    protected override bool FilterMessage(MessageWrapper<UserRegistrationMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.Email);
    }

    protected override async Task<ListenerResponse<IdentityOperationResult>> ProcessMessage(
        MessageWrapper<UserRegistrationMessage> messageWrapper,
        CancellationToken token)
    {
        token.Register(() => Logger.LogInformation("Customer registration operation has been canceled"));

        var registrationModel = messageWrapper.Adapt<EndUserRegistrationModel>();
        var registrationResult = await _identityService.RegisterUser(registrationModel, RoleNames.Customer/*, token*/);

        return new ListenerResponse<IdentityOperationResult>(messageWrapper.Offset, registrationResult);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<UserRegistrationMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to register customer with an email '{Email}'. Retry in {Delay}",
            messageWrapper.Message.Email, delay);
    }
    
    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.CustomersRegistrationChannel;
    }
}