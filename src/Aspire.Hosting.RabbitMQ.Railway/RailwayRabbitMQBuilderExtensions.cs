using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

/// <summary>Publishes standard Aspire RabbitMQ resources to Railway.</summary>
public static class RailwayRabbitMQBuilderExtensions
{
    /// <summary>Deploys private RabbitMQ and an authenticated HTTPS management proxy.</summary>
    /// <param name="builder">The standard local RabbitMQ builder with explicit application credentials.</param>
    /// <param name="target">The recorded, site-owned Railway destination.</param>
    /// <param name="operatorUser">The non-guest operator username parameter.</param>
    /// <param name="operatorPassword">A unique secret operator password containing at least 32 characters.</param>
    /// <param name="configure">Optional broker and proxy configuration.</param>
    /// <returns>The unchanged RabbitMQ resource builder.</returns>
    [AspireExportIgnore(Reason = "C# callbacks are not a stable guest-language transport contract.")]
    public static IResourceBuilder<RabbitMQServerResource> PublishToRailway(
        this IResourceBuilder<RabbitMQServerResource> builder,
        IResourceBuilder<RailwayTargetResource> target,
        IResourceBuilder<ParameterResource> operatorUser,
        IResourceBuilder<ParameterResource> operatorPassword,
        Action<RailwayRabbitMQDeploymentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(operatorUser);
        ArgumentNullException.ThrowIfNull(operatorPassword);
        RailwayRabbitMQDeploymentOptions options = new();
        configure?.Invoke(options);
        options.Validate();

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        ParameterResource userName = builder.Resource.UserNameParameter
            ?? throw new InvalidOperationException("Pass an explicit application username to AddRabbitMQ; guest is not allowed in production.");
        if (!operatorPassword.Resource.Secret || !builder.Resource.PasswordParameter.Secret)
        {
            throw new ArgumentException("Operator and application passwords must be secret Aspire parameters.");
        }

        if (operatorPassword.Resource == builder.Resource.PasswordParameter || operatorUser.Resource == userName)
        {
            throw new ArgumentException("Operator and application credentials must use distinct parameters.");
        }

        builder.WithEnvironment("PAPP_RABBITMQ_APPLICATION_USER", userName)
            .WithEnvironment("PAPP_RABBITMQ_APPLICATION_PASSWORD", builder.Resource.PasswordParameter)
            .WithEnvironment("PAPP_RABBITMQ_OPERATOR_USER", operatorUser)
            .WithEnvironment("PAPP_RABBITMQ_OPERATOR_PASSWORD", operatorPassword)
            .WithEnvironment("RABBITMQ_NODENAME", "rabbit@localhost")
            .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS", "+S 2:2 +A 8");

        RailwayBuilderExtensions.PublishToRailway(builder, target, service =>
        {
            service.ServiceName = options.ServiceName;
            service.Image = options.Image;
            service.StartCommand = RailwayRabbitMQCommands.Broker(options);
            service.Region = options.Region;
            service.MemoryGB = options.MemoryGB;
            service.VCpus = options.VCpus;
            service.PublicDomain = false;
            service.OwnershipMode = options.OwnershipMode;
            service.ExistingServiceId = options.ExistingServiceId;
            service.SealedVariables.Add("PAPP_RABBITMQ_APPLICATION_PASSWORD");
            service.SealedVariables.Add("PAPP_RABBITMQ_OPERATOR_PASSWORD");
            service.SealedVariables.Add("RABBITMQ_DEFAULT_PASS");
            service.Volumes.Add(new RailwayVolumeOptions { MountPath = options.VolumeMountPath });
        });

        IResourceBuilder<ContainerResource> proxy = builder.ApplicationBuilder.AddContainer(builder.Resource.Name + "-management", "nginx")
            .WithEnvironment("PAPP_RABBITMQ_HOST", builder.GetRailwayOutputs().PrivateHostname)
            .PublishToRailway(target, service =>
            {
                service.ServiceName = options.ProxyServiceName;
                service.Image = options.ProxyImage;
                service.StartCommand = RailwayRabbitMQCommands.Proxy();
                service.Region = options.Region;
                service.Port = 8080;
                service.PublicDomain = true;
                service.HealthCheckPath = "/health";
                service.OwnershipMode = options.OwnershipMode;
                service.ExistingServiceId = options.ExistingProxyServiceId;
                service.DeploymentDependsOn.Add(builder.Resource);
                if (options.ManagementDomain is not null)
                {
                    service.CustomDomains.Add(options.ManagementDomain);
                }
            });

        builder.WithAnnotation(new RailwayRabbitMQAnnotation(options, proxy.Resource), ResourceAnnotationMutationBehavior.Replace);
        builder.WithAnnotation(new ConnectionStringRedirectAnnotation(
            new RailwayRabbitMQConnectionResource(builder.Resource, builder.GetRailwayOutputs().PrivateHostname, options.VirtualHost)),
            ResourceAnnotationMutationBehavior.Replace);
        return builder;
    }

    /// <summary>Deploys a standard RabbitMQ resource from a TypeScript AppHost.</summary>
    /// <param name="builder">The standard RabbitMQ resource.</param>
    /// <param name="target">The recorded Railway destination.</param>
    /// <param name="operatorUser">The operator username.</param>
    /// <param name="operatorPassword">The secret operator password.</param>
    /// <param name="options">The serialized configuration.</param>
    /// <returns>The same RabbitMQ builder.</returns>
    [AspireExport("pinguapps.railway.rabbitmq.publishToRailway", MethodName = "publishToRailway")]
    public static IResourceBuilder<RabbitMQServerResource> PublishToRailwayForTypeScript(
        this IResourceBuilder<RabbitMQServerResource> builder,
        IResourceBuilder<RailwayTargetResource> target,
        IResourceBuilder<ParameterResource> operatorUser,
        IResourceBuilder<ParameterResource> operatorPassword,
        RailwayRabbitMQDeploymentOptionsDto? options = null)
    {
        return builder.PublishToRailway(target, operatorUser, operatorPassword, value => options?.CopyTo(value));
    }
}

internal sealed class RailwayRabbitMQAnnotation : IResourceAnnotation
{
    internal RailwayRabbitMQAnnotation(RailwayRabbitMQDeploymentOptions options, ContainerResource proxy)
    {
        Options = options;
        Proxy = proxy;
    }

    internal RailwayRabbitMQDeploymentOptions Options { get; }
    internal ContainerResource Proxy { get; }
}
