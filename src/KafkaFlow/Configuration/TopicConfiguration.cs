using System.Collections.Generic;

namespace KafkaFlow.Configuration;

/// <summary>
/// Represents a Topic configuration
/// </summary>
public class TopicConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TopicConfiguration"/> class.
    /// </summary>
    /// <param name="name">The topic name</param>
    /// <param name="partitions">The number of partitions for the topic</param>
    /// <param name="replicas">Replication factor for the topic</param>
    /// <param name="configs">Additional topic creation configuration values.</param>
    public TopicConfiguration(string name, int partitions, short replicas, Dictionary<string, string> configs)
    {
        this.Name = name;
        this.Partitions = partitions;
        this.Replicas = replicas;
        this.Configs = configs;
    }

    /// <summary>
    /// Gets the Topic Name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the number of Topic Partitions
    /// </summary>
    public int Partitions { get; }

    /// <summary>
    /// Gets the Topic Replication Factor
    /// </summary>
    public short Replicas { get; }

    /// <summary>
    /// Gets the topic creation configuration
    /// </summary>
    public Dictionary<string, string> Configs { get; }
}
