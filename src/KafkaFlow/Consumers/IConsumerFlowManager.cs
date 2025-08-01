using System.Collections.Generic;

namespace KafkaFlow.Consumers;

/// <summary>
/// The consumer flow manager
/// </summary>
public interface IConsumerFlowManager
{
    /// <summary>
    /// Gets a list of the consumer paused partitions
    /// </summary>
    IReadOnlyList<Confluent.Kafka.TopicPartition> PausedPartitions { get; }

    /// <summary>
    /// Pauses a set of partitions
    /// </summary>
    /// <param name="topicPartitions">A list of partitions</param>
    void Pause(IReadOnlyCollection<Confluent.Kafka.TopicPartition> topicPartitions);

    /// <summary>
    /// Resumes a set of partitions
    /// </summary>
    /// <param name="topicPartitions">A list of partitions</param>
    void Resume(IReadOnlyCollection<Confluent.Kafka.TopicPartition> topicPartitions);
}
