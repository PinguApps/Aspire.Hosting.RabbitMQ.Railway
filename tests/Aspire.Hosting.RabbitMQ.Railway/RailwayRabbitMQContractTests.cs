using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.RabbitMQ.Railway;
using Aspire.Hosting.Railway;
using Xunit;

namespace PinguApps.Aspire.Hosting.RabbitMQ.Railway.Tests;

public sealed class RailwayRabbitMQContractTests
{
    [Fact]
    public void LocalRun_PreservesStandardBrokerAndDoesNotCreateProxy()
    {
        IDistributedApplicationBuilder app = DistributedApplication.CreateBuilder();
        IResourceBuilder<RabbitMQServerResource> rabbit = CreateBroker(app);
        IResourceBuilder<RabbitMQServerResource> result = Publish(app, rabbit);
        Assert.Same(rabbit, result);
        Assert.IsType<RabbitMQServerResource>(result.Resource);
        Assert.False(result.Resource.IsExcludedFromPublish());
        Assert.DoesNotContain(app.Resources, resource => resource.Name == "rabbit-management");
    }

    [Fact]
    public void Publish_KeepsResourceOfRecordAndAddsOnlyManagementProxy()
    {
        IDistributedApplicationBuilder app = CreatePublishingBuilder();
        IResourceBuilder<RabbitMQServerResource> rabbit = CreateBroker(app);
        Publish(app, rabbit);
        Assert.IsType<RabbitMQServerResource>(rabbit.Resource);
        Assert.True(rabbit.Resource.IsExcludedFromPublish());
        ContainerResource proxy = Assert.IsType<ContainerResource>(Assert.Single(app.Resources, resource => resource.Name == "rabbit-management"));
        Assert.True(proxy.IsExcludedFromPublish());
        Assert.Single(rabbit.Resource.Annotations.OfType<ConnectionStringRedirectAnnotation>());
        Assert.DoesNotContain("railway-api-token", rabbit.GetRailwayRabbitMQManagementUrl().ValueExpression, StringComparison.Ordinal);
    }

    [Fact]
    public void Publish_RejectsImplicitGuestAndReusedCredentials()
    {
        IDistributedApplicationBuilder app = CreatePublishingBuilder();
        IResourceBuilder<RailwayTargetResource> target = CreateTarget(app);
        IResourceBuilder<ParameterResource> operatorUser = app.AddParameter("operator-user");
        IResourceBuilder<ParameterResource> operatorPassword = app.AddParameter("operator-password", secret: true);
        Assert.Throws<InvalidOperationException>(() => app.AddRabbitMQ("guest-broker").PublishToRailway(target, operatorUser, operatorPassword));
        Assert.Throws<ArgumentException>(() => app.AddRabbitMQ("shared-credentials", operatorUser, operatorPassword).PublishToRailway(target, operatorUser, operatorPassword));
        Assert.Throws<ArgumentException>(() => CreateBroker(app).PublishToRailway(target, operatorUser, app.AddParameter("plaintext-password")));
    }

    [Fact]
    public void Definitions_AssignManagementOnlyToOperatorAndRestrictApplicationTopology()
    {
        RailwayRabbitMQDeploymentOptions options = new() { VirtualHost = "client", ConfigurePermissions = "^uploads\\.", WritePermissions = "^uploads\\.", ReadPermissions = "^uploads\\." };
        string command = RailwayRabbitMQCommands.Broker(options);
        int encodedStart = command.IndexOf("printf", StringComparison.Ordinal);
        string encodedSection = command[encodedStart..];
        string[] segments = encodedSection.Split("'\"'\"'", StringSplitOptions.None);
        string encoded = segments[3];
        using JsonDocument document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
        JsonElement users = document.RootElement.GetProperty("users");
        Assert.Equal("administrator", users[0].GetProperty("tags").GetString());
        Assert.Equal(string.Empty, users[1].GetProperty("tags").GetString());
        Assert.Equal("client", document.RootElement.GetProperty("vhosts")[0].GetProperty("name").GetString());
        Assert.Equal("^uploads\\.", document.RootElement.GetProperty("permissions")[1].GetProperty("read").GetString());
        Assert.Contains("unset RABBITMQ_DEFAULT_USER RABBITMQ_DEFAULT_PASS", command, StringComparison.Ordinal);
        Assert.Contains("-q hash_password", command, StringComparison.Ordinal);
        Assert.Contains("rabbitmq-diagnostics -q ping", command, StringComparison.Ordinal);
        Assert.Contains("rabbitmqctl -q delete_user guest", command, StringComparison.Ordinal);
        Assert.DoesNotContain("railway-api-token", command, StringComparison.Ordinal);
    }

    [Fact]
    public void ProxyCommand_OnlyForwardsManagementHttpAndRetainsAuthentication()
    {
        string command = RailwayRabbitMQCommands.Proxy();
        Assert.Contains(":15672", command, StringComparison.Ordinal);
        Assert.DoesNotContain(":5672", command, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", command, StringComparison.Ordinal);
        Assert.Contains("listen [::]:8080", command, StringComparison.Ordinal);
        Assert.Contains("resolver $dns_server valid=5s", command, StringComparison.Ordinal);
        Assert.Contains("proxy_pass http://\\$rabbitmq_upstream", command, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_RejectsWrongMount()
    {
        RailwayRabbitMQDeploymentOptions options = new() { VolumeMountPath = "/tmp" };
        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void TypeScriptDto_CopiesAllSupportedOptions()
    {
        RailwayRabbitMQDeploymentOptionsDto dto = new()
        {
            Image = "broker@sha256:" + new string('a', 64),
            ProxyImage = "proxy@sha256:" + new string('b', 64),
            ServiceName = "broker-service",
            ProxyServiceName = "proxy-service",
            Region = "ams",
            MemoryGB = 2,
            VCpus = 1,
            VolumeMountPath = "/var/lib/rabbitmq",
            VirtualHost = "client /custom",
            ConfigurePermissions = "^uploads\\.",
            ReadPermissions = "^uploads\\.",
            WritePermissions = "^uploads\\.",
            ManagementDomain = "rabbit.example.com",
            OwnershipMode = RailwayOwnershipMode.ExistingOnly,
            ExistingServiceId = "broker-id",
            ExistingProxyServiceId = "proxy-id",
        };
        RailwayRabbitMQDeploymentOptions options = new();
        dto.CopyTo(options);
        Assert.Equal(dto.Image, options.Image);
        Assert.Equal(dto.ProxyImage, options.ProxyImage);
        Assert.Equal(dto.ServiceName, options.ServiceName);
        Assert.Equal(dto.ProxyServiceName, options.ProxyServiceName);
        Assert.Equal(dto.Region, options.Region);
        Assert.Equal(dto.MemoryGB, options.MemoryGB);
        Assert.Equal(dto.VCpus, options.VCpus);
        Assert.Equal(dto.VolumeMountPath, options.VolumeMountPath);
        Assert.Equal(dto.VirtualHost, options.VirtualHost);
        Assert.Equal(dto.ConfigurePermissions, options.ConfigurePermissions);
        Assert.Equal(dto.ReadPermissions, options.ReadPermissions);
        Assert.Equal(dto.WritePermissions, options.WritePermissions);
        Assert.Equal(dto.ManagementDomain, options.ManagementDomain);
        Assert.Equal(dto.OwnershipMode, options.OwnershipMode);
        Assert.Equal(dto.ExistingServiceId, options.ExistingServiceId);
        Assert.Equal(dto.ExistingProxyServiceId, options.ExistingProxyServiceId);
    }

    [Fact]
    [Trait("Category", "live-railway")]
    public async Task LiveRailway_ProjectCredentialBelongsToRecordedEnvironment()
    {
        string? token = Environment.GetEnvironmentVariable("RAILWAY_API_TOKEN");
        string? project = Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID");
        string? environment = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT_ID");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(environment),
            "Set dedicated RAILWAY_API_TOKEN, RAILWAY_PROJECT_ID, RAILWAY_ENVIRONMENT_ID for read-only live verification.");
        using HttpClient client = new();
        using HttpRequestMessage request = new(HttpMethod.Post, "https://backboard.railway.app/graphql/v2");
        request.Headers.Add("Project-Access-Token", token);
        request.Content = new StringContent("{\"query\":\"query{projectToken{projectId environmentId}}\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(document.RootElement.TryGetProperty("errors", out _));
        JsonElement identity = document.RootElement.GetProperty("data").GetProperty("projectToken");
        Assert.Equal(project, identity.GetProperty("projectId").GetString());
        Assert.Equal(environment, identity.GetProperty("environmentId").GetString());
    }

    private static IDistributedApplicationBuilder CreatePublishingBuilder()
    {
        return DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", Path.Combine(Path.GetTempPath(), "rabbitmq-contract-manifest")]);
    }

    private static IResourceBuilder<RabbitMQServerResource> CreateBroker(IDistributedApplicationBuilder app)
    {
        return app.AddRabbitMQ("rabbit", app.AddParameter("application-user"), app.AddParameter("application-password", secret: true));
    }

    private static IResourceBuilder<RailwayTargetResource> CreateTarget(IDistributedApplicationBuilder app)
    {
        return app.AddRailwayTarget("railway", app.AddParameter("railway-project"), app.AddParameter("railway-environment"), app.AddParameter("railway-api-token", secret: true), app.AddParameter("site-key"));
    }

    private static IResourceBuilder<RabbitMQServerResource> Publish(IDistributedApplicationBuilder app, IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        return rabbit.PublishToRailway(CreateTarget(app), app.AddParameter("operator-user"), app.AddParameter("operator-password", secret: true));
    }
}
