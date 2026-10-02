using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ.Railway;
using Aspire.Hosting.Railway;

namespace PinguApps.Aspire.Hosting.RabbitMQ.Railway.Samples;

internal static class RailwayRabbitMQAppHostSnippets
{
    internal static IResourceBuilder<RabbitMQServerResource> Configure(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<RailwayTargetResource> target = builder.AddRailwayTarget(
            "railway",
            builder.AddParameter("railway-project-id"),
            builder.AddParameter("railway-environment-id"),
            builder.AddParameter("railway-api-token", secret: true),
            builder.AddParameter("site-key"));
        return builder.AddRabbitMQ("rabbitmq",
                builder.AddParameter("rabbitmq-application-user"),
                builder.AddParameter("rabbitmq-application-password", secret: true))
            .PublishToRailway(target,
                builder.AddParameter("rabbitmq-operator-user"),
                builder.AddParameter("rabbitmq-operator-password", secret: true),
                options =>
                {
                    options.Region = "europe-west4-drams3a";
                    options.MemoryGB = 1;
                    options.VCpus = 1;
                });
    }
}
