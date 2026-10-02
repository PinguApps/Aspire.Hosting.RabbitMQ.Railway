using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

internal sealed class RailwayRabbitMQConnectionResource : IResourceWithConnectionString
{
    internal RailwayRabbitMQConnectionResource(RabbitMQServerResource resource, RailwayOutputReference hostname, string virtualHost)
    {
        Name = resource.Name;
        ConnectionStringExpression = ReferenceExpression.Create($"amqp://{resource.UserNameParameter!:uri}:{resource.PasswordParameter:uri}@{hostname}:5672/{Uri.EscapeDataString(virtualHost)}");
    }

    public string Name { get; }
    public ResourceAnnotationCollection Annotations { get; } = [];
    public ReferenceExpression ConnectionStringExpression { get; }
}
