using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

/// <summary>Configures a persistent broker and its HTTPS management proxy.</summary>
public sealed class RailwayRabbitMQDeploymentOptions
{
    /// <summary>Gets or sets the immutable broker image. Defaults to RabbitMQ 4.3 management.</summary>
    public string Image { get; set; } = "rabbitmq@sha256:6ec83ef56205ce7cc6f8a762f28fdeeab50f52e8f79becbac5a3a4c7293a379a";
    /// <summary>Gets or sets the immutable management proxy image. Defaults to Nginx 1.29.6 Alpine.</summary>
    public string ProxyImage { get; set; } = "nginx@sha256:f46cb72c7df02710e693e863a983ac42f6a9579058a59a35f1ae36c9958e4ce0";
    /// <summary>Gets or sets the remote broker service name.</summary>
    public string? ServiceName { get; set; }
    /// <summary>Gets or sets the remote proxy service name.</summary>
    public string? ProxyServiceName { get; set; }
    /// <summary>Gets or sets the Railway region identifier for both services.</summary>
    public string? Region { get; set; }
    /// <summary>Gets or sets the broker memory limit in GB.</summary>
    public double? MemoryGB { get; set; }
    /// <summary>Gets or sets the broker vCPU limit.</summary>
    public double? VCpus { get; set; }
    /// <summary>Gets or sets the broker persistent mount path.</summary>
    public string VolumeMountPath { get; set; } = "/var/lib/rabbitmq";
    /// <summary>Gets or sets the application virtual host.</summary>
    public string VirtualHost { get; set; } = "/";
    /// <summary>Gets or sets the application configure permission expression.</summary>
    public string ConfigurePermissions { get; set; } = "^site\\.";
    /// <summary>Gets or sets the application write permission expression.</summary>
    public string WritePermissions { get; set; } = "^(site\\.|amq\\.default$)";
    /// <summary>Gets or sets the application read permission expression.</summary>
    public string ReadPermissions { get; set; } = "^site\\.";
    /// <summary>Gets or sets a custom management domain. Its DNS is configured separately.</summary>
    public string? ManagementDomain { get; set; }
    /// <summary>Gets or sets the ownership policy shared by broker and proxy.</summary>
    public RailwayOwnershipMode OwnershipMode { get; set; } = RailwayOwnershipMode.CreateOrAdopt;
    /// <summary>Gets or sets the explicit identity when adopting an existing broker.</summary>
    public string? ExistingServiceId { get; set; }
    /// <summary>Gets or sets the explicit identity when adopting an existing management proxy.</summary>
    public string? ExistingProxyServiceId { get; set; }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Image);
        ArgumentException.ThrowIfNullOrWhiteSpace(ProxyImage);
        ArgumentException.ThrowIfNullOrWhiteSpace(VirtualHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(ConfigurePermissions);
        ArgumentException.ThrowIfNullOrWhiteSpace(WritePermissions);
        ArgumentException.ThrowIfNullOrWhiteSpace(ReadPermissions);
        foreach (string value in new[] { VirtualHost, ConfigurePermissions, WritePermissions, ReadPermissions })
        {
            if (value.Contains("PAPP_", StringComparison.Ordinal))
            {
                throw new ArgumentException("RabbitMQ virtual host and permission expressions cannot contain reserved PAPP_ definition markers.");
            }
        }

        if (VolumeMountPath != "/var/lib/rabbitmq")
        {
            throw new ArgumentException("RabbitMQ persistence must mount /var/lib/rabbitmq.", nameof(VolumeMountPath));
        }
    }
}
