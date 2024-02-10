using DistributedBanking.Processing.Domain.Models.Identity;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Identity.Registration;
using Shared.Kafka.Services;
using Shared.Redis.Models;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class WorkerRegistrationListener : BaseListener<string, WorkerRegistrationMessage, IdentityOperationResult>
{
    private readonly IIdentityService _identityService;

    public WorkerRegistrationListener(
        IKafkaConsumerService<string, WorkerRegistrationMessage> workerRegistrationConsumer,
        IIdentityService identityService,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<WorkerRegistrationListener> logger) : base(workerRegistrationConsumer, redisSubscriber, redisProvider, logger)
    {
        _identityService = identityService;
    }

    protected override bool FilterMessage(MessageWrapper<WorkerRegistrationMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.Email);
    }

    protected override async Task<ListenerResponse<IdentityOperationResult>> ProcessMessage(
        MessageWrapper<WorkerRegistrationMessage> messageWrapper)
    {
        var registrationModel = messageWrapper.Message.Adapt<EndUserRegistrationModel>();
        var registrationResult = await _identityService.RegisterUser(registrationModel, messageWrapper.Message.Role);

        return new ListenerResponse<IdentityOperationResult>(messageWrapper.Offset, registrationResult);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay,
        MessageWrapper<WorkerRegistrationMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to register worker with an email '{Email}'. Retry in {Delay}",
            messageWrapper.Message.Email, delay);
    }
    
    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.WorkersRegistrationChannel;
    }
}