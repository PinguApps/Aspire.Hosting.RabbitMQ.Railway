using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

/// <summary>Exposes safe RabbitMQ deployment outputs.</summary>
public static class RailwayRabbitMQOutputBuilderExtensions
{
    /// <summary>Gets the published management URL, or the configured local management endpoint during development.</summary>
    /// <param name="builder">The broker with Railway publishing or a local management plugin configured.</param>
    /// <returns>The deferred management URL without credentials.</returns>
    [AspireExport("pinguapps.railway.rabbitmq.managementUrl", MethodName = "getRailwayRabbitMQManagementUrl")]
    public static ReferenceExpression GetRailwayRabbitMQManagementUrl(this IResourceBuilder<RabbitMQServerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        RailwayRabbitMQAnnotation? annotation = builder.Resource.Annotations.OfType<RailwayRabbitMQAnnotation>().SingleOrDefault();
        if (annotation is null)
        {
            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                if (!builder.Resource.ManagementEndpoint.Exists)
                {
                    throw new InvalidOperationException("Call WithManagementPlugin before requesting the local RabbitMQ management URL.");
                }

                return ReferenceExpression.Create($"{builder.Resource.ManagementEndpoint.Property(EndpointProperty.Url)}");
            }

            throw new InvalidOperationException("Call PublishToRailway before requesting the published RabbitMQ management URL.");
        }

        IResourceBuilder<ContainerResource> proxy = builder.ApplicationBuilder.CreateResourceBuilder(annotation.Proxy);
        return ReferenceExpression.Create($"{proxy.GetRailwayOutputs().PublicUrl}");
    }
}
