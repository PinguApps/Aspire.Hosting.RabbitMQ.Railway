using Aspire.Hosting.Railway;

namespace Aspire.Hosting.RabbitMQ.Railway;

/// <summary>Provides the TypeScript-friendly broker deployment contract.</summary>
[AspireDto]
public sealed class RailwayRabbitMQDeploymentOptionsDto
{
    /// <summary>Gets or sets the immutable broker image.</summary>
    public string? Image { get; set; }
    /// <summary>Gets or sets the immutable proxy image.</summary>
    public string? ProxyImage { get; set; }
    /// <summary>Gets or sets the broker service name.</summary>
    public string? ServiceName { get; set; }
    /// <summary>Gets or sets the proxy service name.</summary>
    public string? ProxyServiceName { get; set; }
    /// <summary>Gets or sets the region.</summary>
    public string? Region { get; set; }
    /// <summary>Gets or sets the broker memory limit.</summary>
    public double? MemoryGB { get; set; }
    /// <summary>Gets or sets the broker CPU limit.</summary>
    public double? VCpus { get; set; }
    /// <summary>Gets or sets the persistence mount path.</summary>
    public string? VolumeMountPath { get; set; }
    /// <summary>Gets or sets the application virtual host.</summary>
    public string? VirtualHost { get; set; }
    /// <summary>Gets or sets application configure permissions.</summary>
    public string? ConfigurePermissions { get; set; }
    /// <summary>Gets or sets application write permissions.</summary>
    public string? WritePermissions { get; set; }
    /// <summary>Gets or sets application read permissions.</summary>
    public string? ReadPermissions { get; set; }
    /// <summary>Gets or sets the management custom domain.</summary>
    public string? ManagementDomain { get; set; }
    /// <summary>Gets or sets the service ownership policy.</summary>
    public RailwayOwnershipMode? OwnershipMode { get; set; }
    /// <summary>Gets or sets the existing broker identity.</summary>
    public string? ExistingServiceId { get; set; }
    /// <summary>Gets or sets the existing proxy identity.</summary>
    public string? ExistingProxyServiceId { get; set; }

    internal void CopyTo(RailwayRabbitMQDeploymentOptions target)
    {
        target.Image = Image ?? target.Image;
        target.ProxyImage = ProxyImage ?? target.ProxyImage;
        target.ServiceName = ServiceName;
        target.ProxyServiceName = ProxyServiceName;
        target.Region = Region;
        target.MemoryGB = MemoryGB;
        target.VCpus = VCpus;
        target.VolumeMountPath = VolumeMountPath ?? target.VolumeMountPath;
        target.VirtualHost = VirtualHost ?? target.VirtualHost;
        target.ConfigurePermissions = ConfigurePermissions ?? target.ConfigurePermissions;
        target.WritePermissions = WritePermissions ?? target.WritePermissions;
        target.ReadPermissions = ReadPermissions ?? target.ReadPermissions;
        target.ManagementDomain = ManagementDomain;
        target.OwnershipMode = OwnershipMode ?? target.OwnershipMode;
        target.ExistingServiceId = ExistingServiceId;
        target.ExistingProxyServiceId = ExistingProxyServiceId;
    }
}
