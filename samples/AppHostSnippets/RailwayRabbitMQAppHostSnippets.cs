using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ.Railway;
using Aspire.Hosting.Railway;

namespace PinguApps.Aspire.Hosting.RabbitMQ.Railway.Samples;

internal static class RailwayRabbitMQAppHostSnippets
{
    internal static IResourceBuilder<RabbitMQServerResource> Configure(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource>? applicationUser = null;
        IResourceBuilder<ParameterResource>? applicationPassword = null;
        if (builder.ExecutionContext.IsPublishMode)
        {
            applicationUser = builder.AddParameter("rabbitmq-application-user");
            applicationPassword = builder.AddParameter("rabbitmq-application-password", secret: true);
        }

        IResourceBuilder<RabbitMQServerResource> rabbit = builder.AddRabbitMQ("rabbitmq", applicationUser, applicationPassword)
            .WithManagementPlugin();

        if (builder.ExecutionContext.IsPublishMode)
        {
            IResourceBuilder<RailwayTargetResource> target = builder.AddRailwayTarget(
                "railway",
                builder.AddParameter("railway-project-id"),
                builder.AddParameter("railway-environment-id"),
                builder.AddParameter("railway-api-token", secret: true),
                builder.AddParameter("site-key"));
            rabbit.PublishToRailway(target,
                builder.AddParameter("rabbitmq-operator-user"),
                builder.AddParameter("rabbitmq-operator-password", secret: true),
                options =>
                {
                    options.Region = "europe-west4-drams3a";
                    options.MemoryGB = 1;
                    options.VCpus = 1;
                });
        }

        return rabbit;
    }
}
