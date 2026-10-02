using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

/// <summary>Exposes safe RabbitMQ deployment outputs.</summary>
public static class RailwayRabbitMQOutputBuilderExtensions
{
    /// <summary>Gets the authenticated public management URL after publication.</summary>
    /// <param name="builder">The published broker.</param>
    /// <returns>The deferred HTTPS management URL without credentials.</returns>
    [AspireExport("pinguapps.railway.rabbitmq.managementUrl", MethodName = "getRailwayRabbitMQManagementUrl")]
    public static ReferenceExpression GetRailwayRabbitMQManagementUrl(this IResourceBuilder<RabbitMQServerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        RailwayRabbitMQAnnotation annotation = builder.Resource.Annotations.OfType<RailwayRabbitMQAnnotation>().Single();
        IResourceBuilder<ContainerResource> proxy = builder.ApplicationBuilder.CreateResourceBuilder(annotation.Proxy);
        return ReferenceExpression.Create($"{proxy.GetRailwayOutputs().PublicUrl}");
    }
}
