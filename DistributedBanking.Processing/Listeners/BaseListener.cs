using DistributedBanking.Processing.Models;
using Shared.Kafka.Messages;
using Shared.Kafka.Services;
using Shared.Messaging.Messages;
using Shared.Redis.Services;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace DistributedBanking.Processing.Listeners;

public abstract class BaseListener<TMessageKey, TMessageValue, TResponse> : BackgroundService where TMessageValue : MessageBase
{
    private const int MaxDelaySeconds = 60;
    private readonly IKafkaConsumerService<TMessageKey, TMessageValue> _consumer;
    private readonly IRedisSubscriber _redisSubscriber;
    private readonly IRedisProvider _redisProvider;
    protected readonly ILogger<BaseListener<TMessageKey, TMessageValue, TResponse>> Logger;

    protected BaseListener(
        IKafkaConsumerService<TMessageKey, TMessageValue> workerRegistrationConsumer,
        IRedisSubscriber redisSubscriber,
        IRedisProvider redisProvider,
        ILogger<BaseListener<TMessageKey, TMessageValue, TResponse>> logger)
    {
        _consumer = workerRegistrationConsumer;
        _redisSubscriber = redisSubscriber;
        _redisProvider = redisProvider;
        Logger = logger;
    }

    protected virtual bool FilterMessage(MessageWrapper<TMessageValue> message)
    {
        return true;
    }
    
    protected abstract Task<ListenerResponse<TResponse>> ProcessMessage(MessageWrapper<TMessageValue> message);

    protected virtual void OnMessageProcessingException(Exception exception, TimeSpan delay, MessageWrapper<TMessageValue> message)
    {
        Logger.LogError(exception, "Error while trying to process '{MessageType}' message. Retry in {Delay}",
            typeof(TMessageValue).Name, delay);
    }
    
    protected virtual async Task OnMessageResponse(ListenerResponse<TResponse> listenerResponse)
    {
        var responseChannel = listenerResponse.ResponseChannel;
        await _redisProvider.SetAsync(responseChannel, listenerResponse.Response, TimeSpan.FromMinutes(5)); // todo this is temp
        await _redisSubscriber.PubAsync(responseChannel, listenerResponse.Response);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() =>
        {
            Logger.LogInformation("Listener {Listener} has received a stop signal", this.GetType().Name);
        });

        _consumer
            .Consume(stoppingToken)
            .Do(_ => Logger.LogInformation("Listener {Listener} has received a message", this.GetType().Name))
            .Where(FilterMessage)
            .Select(message => Observable.FromAsync(async () =>
                {
                    var listenerResponse = await ProcessMessage(message);
                    await OnMessageResponse(listenerResponse);
                })
                .RetryWhen(errors => errors.SelectMany((exception, retry) => 
                {
                    var delay = TimeSpan.FromSeconds(Math.Max(MaxDelaySeconds, retry * 2));
                    OnMessageProcessingException(exception, delay, message);
                    return Observable.Timer(delay); 
                }))
            )
            .Merge() //todo consider
            .RetryWhen(errors => errors.SelectMany((exception, retry) => 
            {
                var delay = TimeSpan.FromSeconds(Math.Max(MaxDelaySeconds, retry * 10));
                Logger.LogError(exception, "Error while listening to '{MessageType}' messages. Retry in {Delay} seconds", 
                    typeof(TMessageValue).Name, delay);
                
                return Observable.Timer(delay);
            }))
            .SubscribeOn(TaskPoolScheduler.Default)
            .Subscribe(
                onNext: _ => { Logger.LogInformation("{Listener} has processed a message", this.GetType().Name);  },
                onError: exception => 
                { 
                    Logger.LogError(exception, "An unexpected error occurred while listening to '{MessageType}' messages", typeof(TMessageValue).Name);
                },
                onCompleted: () => { Logger.LogInformation("{Listener} has ended its work", this.GetType().Name); },
                token: stoppingToken);

        return Task.CompletedTask;
    }
}