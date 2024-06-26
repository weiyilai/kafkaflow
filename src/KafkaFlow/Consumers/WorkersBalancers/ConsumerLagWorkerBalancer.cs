using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KafkaFlow.Clusters;
using KafkaFlow.Configuration;

namespace KafkaFlow.Consumers.WorkersBalancers;

/// <summary>
/// Represents a balancer that dynamically calculates the number of workers for a consumer based on the current lag.
/// The calculation employs a simple rule of three considering the total lag across all application instances,
/// the lag specific to the current instance, and a predetermined total number of consumer workers.
/// </summary>
internal class ConsumerLagWorkerBalancer
{
    private const int DefaultWorkersCount = 1;

    private readonly IClusterManager _clusterManager;
    private readonly IConsumerAccessor _consumerAccessor;
    private readonly ILogHandler _logHandler;
    private readonly int _totalConsumerWorkers;
    private readonly int _minInstanceWorkers;
    private readonly int _maxInstanceWorkers;

    public ConsumerLagWorkerBalancer(
        IClusterManager clusterManager,
        IConsumerAccessor consumerAccessor,
        ILogHandler logHandler,
        int totalConsumerWorkers,
        int minInstanceWorkers,
        int maxInstanceWorkers)
    {
        _clusterManager = clusterManager;
        _consumerAccessor = consumerAccessor;
        _logHandler = logHandler;
        _totalConsumerWorkers = totalConsumerWorkers;
        _minInstanceWorkers = minInstanceWorkers;
        _maxInstanceWorkers = maxInstanceWorkers;
    }

    public async Task<int> GetWorkersCountAsync(WorkersCountContext context)
    {
        var workers = await this.CalculateAsync(context).ConfigureAwait(false);

        _logHandler.Info(
            "New workers count calculated",
            new
            {
                Workers = workers,
                Consumer = context.ConsumerName,
            });

        return workers;
    }

    private static long CalculateMyPartitionsLag(
        WorkersCountContext context,
        IEnumerable<(string Topic, int Partition, long Lag)> partitionsLag)
    {
        return partitionsLag
            .Where(
                partitionLag => context.AssignedTopicsPartitions
                    .Any(
                        topic => topic.Name == partitionLag.Topic &&
                                 topic.Partitions.Any(p => p == partitionLag.Partition)))
            .Sum(partitionLag => partitionLag.Lag);
    }

    private static IReadOnlyList<(string Topic, int Partition, long Lag)> CalculatePartitionsLag(
        IEnumerable<(string Topic, int Partition, long Offset)> lastOffsets,
        IEnumerable<TopicPartitionOffset> currentPartitionsOffset)
    {
        return lastOffsets
            .Select(
                last =>
                {
                    var currentOffset = currentPartitionsOffset
                        .Where(current => current.Topic == last.Topic && current.Partition == last.Partition)
                        .Select(current => current.Offset)
                        .FirstOrDefault();

                    var lastOffset = Math.Max(0, last.Offset);
                    currentOffset = Math.Max(0, currentOffset);

                    return (last.Topic, last.Partition, lastOffset - currentOffset);
                })
            .ToList();
    }

    private async Task<int> CalculateAsync(WorkersCountContext context)
    {
        try
        {
            if (!context.AssignedTopicsPartitions.Any())
            {
                return DefaultWorkersCount;
            }

            var topicsMetadata = await this.GetTopicsMetadataAsync(context).ConfigureAwait(false);

            var lastOffsets = this.GetPartitionsLastOffset(context.ConsumerName, topicsMetadata);

            var partitionsOffset = await _clusterManager.GetConsumerGroupOffsetsAsync(
                context.ConsumerGroupId,
                context.AssignedTopicsPartitions.Select(t => t.Name))
                .ConfigureAwait(false);

            var partitionsLag = CalculatePartitionsLag(lastOffsets, partitionsOffset);
            var instanceLag = CalculateMyPartitionsLag(context, partitionsLag);

            decimal totalConsumerLag = partitionsLag.Sum(p => p.Lag);

            var ratio = instanceLag / Math.Max(1, totalConsumerLag);

            var workers = (int)Math.Round(_totalConsumerWorkers * ratio);

            workers = Math.Min(workers, _maxInstanceWorkers);
            workers = Math.Max(workers, _minInstanceWorkers);

            return workers;
        }
        catch (Exception e)
        {
            _logHandler.Error(
                "Error calculating new workers count, using 1 as fallback",
                e,
                new
                {
                    context.ConsumerName,
                });

            return DefaultWorkersCount;
        }
    }

    private IEnumerable<(string TopicName, int Partition, long Offset)> GetPartitionsLastOffset(
        string consumerName,
        IEnumerable<(string Name, TopicMetadata Metadata)> topicsMetadata)
    {
        var consumer = _consumerAccessor[consumerName];

        return topicsMetadata.SelectMany(
            topic => topic.Metadata.Partitions.Select(
                partition => (
                    topic.Name,
                    partition.Id,
                    consumer
                        .QueryWatermarkOffsets(new(topic.Name, new(partition.Id)), TimeSpan.FromSeconds(30))
                        .High
                        .Value)));
    }

    private async Task<IReadOnlyList<(string Name, TopicMetadata Metadata)>> GetTopicsMetadataAsync(WorkersCountContext context)
    {
        var topicsMetadata = new List<(string Name, TopicMetadata Metadata)>(context.AssignedTopicsPartitions.Count);

        foreach (var topic in context.AssignedTopicsPartitions)
        {
            topicsMetadata.Add((topic.Name, await _clusterManager.GetTopicMetadataAsync(topic.Name).ConfigureAwait(false)));
        }

        return topicsMetadata;
    }
}
