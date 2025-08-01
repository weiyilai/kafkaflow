using System;
using System.Collections.Generic;
using Confluent.Kafka;
using KafkaFlow.Clusters;
using KafkaFlow.Configuration;
using KafkaFlow.Consumers;
using KafkaFlow.Consumers.WorkersBalancers;
using KafkaFlow.Middlewares.Compressor;
using KafkaFlow.Middlewares.TypedHandler;

namespace KafkaFlow;

/// <summary>
/// Provides extension methods over <see cref="IConsumerConfigurationBuilder"/> and <see cref="IProducerConfigurationBuilder"/>
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Sets configurations in the producer based on a <see cref="P:Confluent.Kafka.ProducerConfig"/> instance
    /// </summary>
    /// <param name="builder">A class that implements <see cref="IProducerConfigurationBuilder"/></param>
    /// <param name="config"><see cref="P:Confluent.Kafka.ProducerConfig"/> instance</param>
    /// <returns></returns>
    public static IProducerConfigurationBuilder WithProducerConfig(this IProducerConfigurationBuilder builder, ProducerConfig config)
    {
        return ((ProducerConfigurationBuilder)builder).WithProducerConfig(config);
    }

    /// <summary>
    /// Sets compression configurations in the producer
    /// </summary>
    /// <param name="builder">A class that implements <see cref="IProducerConfigurationBuilder"/></param>
    /// <param name="compressionType">
    /// <see cref="P:Confluent.Kafka.CompressionType"/> enum to select the compression codec to use for compressing message sets. This is the default value for all topics, may be overridden by the topic configuration property `compression.codec`.
    /// default: none
    /// importance: medium</param>
    /// <param name="compressionLevel">
    /// Compression level parameter for algorithm selected by <see cref="P:Confluent.Kafka.CompressionType"/> enum. Higher values will result in better compression at the cost of more CPU usage. Usable range is algorithm-dependent: [0-9] for gzip; [0-12] for lz4; only 0 for snappy; -1 = codec-dependent default compression level.
    /// default: -1
    /// importance: medium
    /// </param>
    /// <returns></returns>
    public static IProducerConfigurationBuilder WithCompression(
        this IProducerConfigurationBuilder builder,
        CompressionType compressionType,
        int? compressionLevel = -1)
    {
        return ((ProducerConfigurationBuilder)builder).WithCompression(compressionType, compressionLevel);
    }

    /// <summary>
    /// Sets configurations in the consumer based on a <see cref="P:Confluent.Kafka.ConsumerConfig"/> instance
    /// </summary>
    /// <param name="builder">A class that implements <see cref="IConsumerConfigurationBuilder"/></param>
    /// <param name="config"><see cref="P:Confluent.Kafka.ConsumerConfig"/> instance</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithConsumerConfig(this IConsumerConfigurationBuilder builder, ConsumerConfig config)
    {
        return ((ConsumerConfigurationBuilder)builder).WithConsumerConfig(config);
    }

    /// <summary>
    /// Adds a handler for the Kafka consumer partitions assigned
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="partitionsAssignedHandler">A handler for the consumer partitions assigned</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithPartitionsAssignedHandler(
        this IConsumerConfigurationBuilder builder,
        Action<IDependencyResolver, List<Confluent.Kafka.TopicPartition>> partitionsAssignedHandler)
    {
        return ((ConsumerConfigurationBuilder)builder).WithPartitionsAssignedHandler(partitionsAssignedHandler);
    }

    /// <summary>
    /// Adds a handler for the Kafka consumer partitions revoked
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="partitionsRevokedHandler">A handler for the consumer partitions revoked</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithPartitionsRevokedHandler(
        this IConsumerConfigurationBuilder builder,
        Action<IDependencyResolver, List<Confluent.Kafka.TopicPartitionOffset>> partitionsRevokedHandler)
    {
        return ((ConsumerConfigurationBuilder)builder).WithPartitionsRevokedHandler(partitionsRevokedHandler);
    }

    /// <summary>
    /// Adds a handler to be executed when KafkaFlow has pending offsets to be committed
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="pendingOffsetsHandler">A handler for the consumer pending offsets state</param>
    /// <param name="interval">The time interval between handler executions</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithPendingOffsetsStatisticsHandler(
        this IConsumerConfigurationBuilder builder,
        Action<IDependencyResolver, IEnumerable<Confluent.Kafka.TopicPartitionOffset>> pendingOffsetsHandler,
        TimeSpan interval)
    {
        return ((ConsumerConfigurationBuilder)builder).WithPendingOffsetsStatisticsHandler(pendingOffsetsHandler, interval);
    }

    /// <summary>
    /// Register a custom consumer factory to be internally used by the framework
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="decoratorFactory">The factory method</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithCustomFactory(
        this IConsumerConfigurationBuilder builder,
        ConsumerCustomFactory decoratorFactory)
    {
        return ((ConsumerConfigurationBuilder)builder).WithCustomFactory(decoratorFactory);
    }

    /// <summary>
    /// Register a custom producer factory to be internally used by the framework
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="decoratorFactory">The factory method</param>
    /// <returns></returns>
    public static IProducerConfigurationBuilder WithCustomFactory(
        this IProducerConfigurationBuilder builder,
        ProducerCustomFactory decoratorFactory)
    {
        return ((ProducerConfigurationBuilder)builder).WithCustomFactory(decoratorFactory);
    }

    /// <summary>
    /// Configures the consumer to use the consumer's lag as a metric for dynamically calculating the number of workers for each application instance.
    /// </summary>
    /// <param name="builder">The consumer's configuration builder.</param>
    /// <param name="totalWorkers">The total number of workers to be distributed across all application instances. The sum of workers across all instances will approximate this number.</param>
    /// <param name="minInstanceWorkers">The minimum number of workers for each application instance.</param>
    /// <param name="maxInstanceWorkers">The maximum number of workers for each application instance.</param>
    /// <param name="evaluationInterval">The interval at which the number of workers will be recalculated based on consumer's lag.</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithConsumerLagWorkerBalancer(
        this IConsumerConfigurationBuilder builder,
        int totalWorkers,
        int minInstanceWorkers,
        int maxInstanceWorkers,
        TimeSpan evaluationInterval)
    {
        return builder.WithWorkersCount(
            (context, resolver) =>
                new ConsumerLagWorkerBalancer(
                        resolver.Resolve<IClusterManager>(),
                        resolver.Resolve<IConsumerAccessor>(),
                        resolver.Resolve<ILogHandler>(),
                        totalWorkers,
                        minInstanceWorkers,
                        maxInstanceWorkers)
                    .GetWorkersCountAsync(context),
            evaluationInterval);
    }

    /// <summary>
    /// Configures the consumer to use the consumer's lag as a metric for dynamically calculating the number of workers for each application instance.
    /// The number of workers will be re-evaluated every 5 minutes.
    /// </summary>
    /// <param name="builder">The consumer's configuration builder.</param>
    /// <param name="totalWorkers">The total number of workers to be distributed across all application instances. The sum of workers across all instances will approximate this number.</param>
    /// <param name="minInstanceWorkers">The minimum number of workers for each application instance.</param>
    /// <param name="maxInstanceWorkers">The maximum number of workers for each application instance.</param>
    /// <returns></returns>
    public static IConsumerConfigurationBuilder WithConsumerLagWorkerBalancer(
        this IConsumerConfigurationBuilder builder,
        int totalWorkers,
        int minInstanceWorkers,
        int maxInstanceWorkers)
    {
        return builder.WithConsumerLagWorkerBalancer(
            totalWorkers,
            minInstanceWorkers,
            maxInstanceWorkers,
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Adds typed handler middleware
    /// </summary>
    /// <param name="builder">Instance of <see cref="IConsumerMiddlewareConfigurationBuilder"/></param>
    /// <param name="configure">A handler to configure the middleware</param>
    /// <returns></returns>
    public static IConsumerMiddlewareConfigurationBuilder AddTypedHandlers(
        this IConsumerMiddlewareConfigurationBuilder builder,
        Action<TypedHandlerConfigurationBuilder> configure)
    {
        var typedHandlerBuilder = new TypedHandlerConfigurationBuilder(builder.DependencyConfigurator);

        configure(typedHandlerBuilder);

        var configuration = typedHandlerBuilder.Build();

        builder.Add(
            resolver => new TypedHandlerMiddleware(resolver, configuration),
            MiddlewareLifetime.Message);

        return builder;
    }

    /// <summary>
    /// Registers a middleware to decompress the message
    /// </summary>
    /// <param name="middlewares">The middleware configuration builder</param>
    /// <typeparam name="T">The compressor type</typeparam>
    /// <returns></returns>
    public static IConsumerMiddlewareConfigurationBuilder AddDecompressor<T>(this IConsumerMiddlewareConfigurationBuilder middlewares)
        where T : class, IDecompressor
    {
        middlewares.DependencyConfigurator.AddTransient<T>();
        return middlewares.AddDecompressor(resolver => resolver.Resolve<T>());
    }

    /// <summary>
    /// Registers a middleware to decompress the message
    /// </summary>
    /// <param name="middlewares">The middleware configuration builder</param>
    /// <typeparam name="T">The decompressor type that implements <see cref="IDecompressor"/></typeparam>
    /// <param name="factory">A factory to create the <see cref="IDecompressor"/> instance</param>
    /// <returns></returns>
    public static IConsumerMiddlewareConfigurationBuilder AddDecompressor<T>(
        this IConsumerMiddlewareConfigurationBuilder middlewares,
        Factory<T> factory)
        where T : class, IDecompressor
    {
        return middlewares.Add(resolver => new DecompressorConsumerMiddleware(factory(resolver)));
    }

    /// <summary>
    /// Registers a middleware to compress the message
    /// It is highly recommended to use the producer native compression ('WithCompression()' method) instead of using the compressor middleware
    /// </summary>
    /// <param name="middlewares">The middleware configuration builder</param>
    /// <typeparam name="T">The compressor type that implements <see cref="ICompressor"/></typeparam>
    /// <returns></returns>
    public static IProducerMiddlewareConfigurationBuilder AddCompressor<T>(this IProducerMiddlewareConfigurationBuilder middlewares)
        where T : class, ICompressor
    {
        middlewares.DependencyConfigurator.AddTransient<T>();
        return middlewares.AddCompressor(resolver => resolver.Resolve<T>());
    }

    /// <summary>
    /// Registers a middleware to compress the message
    /// It is highly recommended to use the producer native compression ('WithCompression()' method) instead of using the compressor middleware
    /// </summary>
    /// <param name="middlewares">The middleware configuration builder</param>
    /// <typeparam name="T">The compressor type that implements <see cref="ICompressor"/></typeparam>
    /// <param name="factory">A factory to create the <see cref="ICompressor"/> instance</param>
    /// <returns></returns>
    public static IProducerMiddlewareConfigurationBuilder AddCompressor<T>(
        this IProducerMiddlewareConfigurationBuilder middlewares,
        Factory<T> factory)
        where T : class, ICompressor
    {
        return middlewares.Add(resolver => new CompressorProducerMiddleware(factory(resolver)));
    }
}
