using Contracts;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Identity;
using Shared.Kafka.Services;
using Shared.Redis.Models;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class EndUserDeletionListener : BaseListener<string, EndUserDeletionMessage, OperationStatusModel>
{
    private readonly IIdentityService _identityService;

    public EndUserDeletionListener(
        IKafkaConsumerService<string, EndUserDeletionMessage> endUserDeletionConsumer,
        IIdentityService identityService,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<EndUserDeletionListener> logger) : base(endUserDeletionConsumer, redisSubscriber, redisProvider, logger)
    {
        _identityService = identityService;
    }

    protected override bool FilterMessage(MessageWrapper<EndUserDeletionMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.EndUserId);
    }

    protected override async Task<ListenerResponse<OperationStatusModel>> ProcessMessage(
        MessageWrapper<EndUserDeletionMessage> messageWrapper)
    {
        var deletionResult = await _identityService.DeleteUser(messageWrapper.Message.EndUserId); //todo inconsistency in email and ids

        return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, deletionResult);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<EndUserDeletionMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to delete end user with an ID '{EndUserId}'. Retry in {Delay}",
            messageWrapper.Message.EndUserId, delay);
    }
    
    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.UsersDeletionChannel;
    }
}