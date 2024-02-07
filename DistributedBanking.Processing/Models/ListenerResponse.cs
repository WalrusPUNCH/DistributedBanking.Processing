using Confluent.Kafka;
using Shared.Kafka.Messages;

namespace DistributedBanking.Processing.Models;

public record ListenerResponse<TResponse>(TopicPartitionOffset MessageOffset, TResponse Response);