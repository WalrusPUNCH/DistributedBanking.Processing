using Contracts.Models;
using DistributedBanking.Processing.Domain.Models.Identity;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages.Identity;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class CustomerInformationUpdateListener : BaseListener<string, CustomerInformationUpdateMessage, OperationResult>
{
    private readonly IIdentityService _identityService;

    public CustomerInformationUpdateListener(
        IKafkaConsumerService<string, CustomerInformationUpdateMessage> informationUpdateConsumer,
        IIdentityService identityService,
        IRedisSubscriber redisSubscriber,
        ILogger<CustomerInformationUpdateListener> logger) : base(informationUpdateConsumer, redisSubscriber, logger)
    {
        _identityService = identityService;
    }

    protected override bool FilterMessage(MessageWrapper<CustomerInformationUpdateMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.CustomerId);
    }

    protected override async Task<ListenerResponse<OperationResult>> ProcessMessage(
        MessageWrapper<CustomerInformationUpdateMessage> messageWrapper)
    {
        var updatedInformationModel = messageWrapper.Message.Adapt<CustomerPassportModel>();
        var updateResult =  await _identityService.UpdateCustomerPersonalInformation(messageWrapper.Message.CustomerId, updatedInformationModel);

        return new ListenerResponse<OperationResult>(
            MessageOffset: messageWrapper.Offset,
            Response: updateResult,
            ResponseChannelPattern: messageWrapper.Message.ResponseChannelPattern);
    }

    protected override void OnMessageProcessingException(
        Exception exception, 
        TimeSpan delay, 
        MessageWrapper<CustomerInformationUpdateMessage> messageWrapper)
    {
        Logger.LogError(exception, 
            "Error while trying to update customer information for customer '{CustomerId}'. Retry in {Delay}", 
            messageWrapper.Message.CustomerId, delay);
    }
}