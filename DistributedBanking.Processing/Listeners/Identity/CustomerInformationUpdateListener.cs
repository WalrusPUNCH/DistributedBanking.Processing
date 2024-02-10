using Contracts;
using DistributedBanking.Processing.Domain.Models.Identity;
using DistributedBanking.Processing.Domain.Services;
using DistributedBanking.Processing.Models;
using Mapster;
using Shared.Kafka.Messages;
using Shared.Kafka.Messages.Identity;
using Shared.Kafka.Services;
using Shared.Redis.Models;
using Shared.Redis.Services;

namespace DistributedBanking.Processing.Listeners.Identity;

public class CustomerInformationUpdateListener : BaseListener<string, CustomerInformationUpdateMessage, OperationStatusModel>
{
    private readonly IIdentityService _identityService;

    public CustomerInformationUpdateListener(
        IKafkaConsumerService<string, CustomerInformationUpdateMessage> informationUpdateConsumer,
        IIdentityService identityService,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<CustomerInformationUpdateListener> logger) : base(informationUpdateConsumer, redisSubscriber, redisProvider, logger)
    {
        _identityService = identityService;
    }

    protected override bool FilterMessage(MessageWrapper<CustomerInformationUpdateMessage> messageWrapper)
    {
        return base.FilterMessage(messageWrapper) && !string.IsNullOrWhiteSpace(messageWrapper.Message.CustomerId);
    }

    protected override async Task<ListenerResponse<OperationStatusModel>> ProcessMessage(
        MessageWrapper<CustomerInformationUpdateMessage> messageWrapper)
    {
        var updatedInformationModel = messageWrapper.Adapt<CustomerPassportModel>();
        var updateResult =  await _identityService.UpdateCustomerPersonalInformation(messageWrapper.Message.CustomerId, updatedInformationModel);

        return new ListenerResponse<OperationStatusModel>(messageWrapper.Offset, updateResult);
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
    
    protected override string RedisChannelBaseForResponse()
    {
        return RedisChannelConstants.CustomersUpdateChannel;
    }
}