using Contracts.Models;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages.Identity;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class EndUserDeletionListener : BaseListener<string, EndUserDeletionMessage, OperationResult>
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

    protected override async Task<ListenerResponse<OperationResult>> ProcessMessage(
        MessageWrapper<EndUserDeletionMessage> messageWrapper)
    {
        var deletionResult = await _identityService.DeleteUser(messageWrapper.Message.EndUserId);

        return new ListenerResponse<OperationResult>(
            MessageOffset: messageWrapper.Offset,
            Response: deletionResult,
            ResponseChannelPattern: messageWrapper.Message.ResponseChannelPattern);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<EndUserDeletionMessage> messageWrapper)
    {
        Logger.LogError(exception, "Error while trying to delete end user with an ID '{EndUserId}'. Retry in {Delay}",
            messageWrapper.Message.EndUserId, delay);
    }
}