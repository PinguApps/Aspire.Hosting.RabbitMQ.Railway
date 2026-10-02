# PinguApps.Aspire.Hosting.RabbitMQ.Railway

Publish standard Aspire RabbitMQ resources to a site-owned Railway environment. Local development continues to use the standard RabbitMQ container. Deployment creates one persistent private broker and one public HTTPS management proxy.

## Install

```powershell
dotnet add package Aspire.Hosting.RabbitMQ --version 13.6.0
dotnet add package PinguApps.Aspire.Hosting.RabbitMQ.Railway --version 1.0.0
```

The package depends on `PinguApps.Aspire.Hosting.Railway` 1.0.0. Use .NET 10 and Aspire CLI 13.6.0.

## C# AppHost

```csharp
using Aspire.Hosting.RabbitMQ.Railway;
using Aspire.Hosting.Railway;
using Aspire.Hosting.ApplicationModel;

IResourceBuilder<ParameterResource>? applicationUser = null;
IResourceBuilder<ParameterResource>? applicationPassword = null;
if (builder.ExecutionContext.IsPublishMode)
{
    applicationUser = builder.AddParameter("rabbitmq-application-user");
    applicationPassword = builder.AddParameter("rabbitmq-application-password", secret: true);
}

var rabbit = builder.AddRabbitMQ("rabbitmq", applicationUser, applicationPassword)
    .WithManagementPlugin();
var worker = builder.AddProject<Projects.Worker>("worker").WithReference(rabbit);

if (builder.ExecutionContext.IsPublishMode)
{
    var target = builder.AddRailwayTarget("railway",
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
    worker.PublishToRailway(target, options => options.Image = release.WorkerImage);
}
```

Run `aspire deploy`. The target requires a pre-created project and environment, an environment-scoped project token, and the matching shared `PINGUAPPS_SITE_KEY` allocation marker. The extension delegates ownership, drift detection, retained-image deployment, and recovery to the shared Railway publisher.

Create Railway targets and deployment parameters only inside `IsPublishMode`. Ordinary broker/workload declarations stay outside so local development requires no production credentials. The TypeScript equivalent is `await (await builder.executionContext()).isPublishMode()`.

Explicit existing service identities are required when adopting previously unmarked infrastructure. No resource or volume is automatically deleted.

## TypeScript AppHost

```json
{
  "packages": {
    "Aspire.Hosting.RabbitMQ": "13.6.0",
    "PinguApps.Aspire.Hosting.Railway": "1.0.0",
    "PinguApps.Aspire.Hosting.RabbitMQ.Railway": "1.0.0"
  }
}
```

Run `aspire restore` to generate bindings. The callback-free exported `publishToRailway(target, operatorUser, operatorPassword, options)` accepts `RailwayRabbitMQDeploymentOptionsDto`; see [the runnable fixture](samples/TypeScriptAppHost/apphost.mts). CI packs the NuGet package, restores a separate TypeScript fixture from that package, typechecks it, and inspects the publish pipeline.

## Security and persistence

- AMQP port 5672 and broker management port 15672 are private Railway networking destinations. Only Nginx port 8080 gets public HTTPS.
- RabbitMQ authenticates management UI/API requests. The proxy adds no separate identity provider. Its `/health` route reveals no broker state.
- Operator and application usernames must be distinct, non-`guest` identifiers using letters, digits, dots, hyphens, or underscores. Usernames, virtual host, and permission expressions cannot contain reserved `PAPP_` definition markers.
- Passwords must be distinct secret parameters, at least 32 characters. Generate independent random passwords and persist them in your deployment secret store; redeployment must reuse them.
- The operator has RabbitMQ administrator permissions. Application credentials have no management tags and default permissions only for `site.*` resources and the default exchange. Override permission expressions for your declared topology.
- Password hashes are generated inside the broker at boot and definitions are imported before service access. Railway's deployment token is never injected into broker, proxy, or consumer configuration.
- Broker password variables are sealed in Railway. Consumers should seal their credential-bearing variables through core `SealedVariables`. Protect Aspire deployment state: Aspire caches parameter values locally.
- Persist `/var/lib/rabbitmq` with stable `rabbit@localhost` node identity. A single broker and one volume are not high availability.
- Runtime AMQP credentials belong only in permitted application services. Durable queues, publisher confirms, manual acknowledgements, retries, dead letters, and idempotent processing remain consuming application responsibilities.

## Configuration

`Image` and `ProxyImage` are immutable digest references. `ServiceName`, `ProxyServiceName`, `Region`, `MemoryGB`, `VCpus`, `VirtualHost`, configure/write/read permission expressions, management custom domain, ownership mode, and explicit broker/proxy adoption identities are configurable. The persistent mount must remain `/var/lib/rabbitmq`.

`WithReference(rabbit)` uses application credentials and the private broker endpoint during publishing. A configured virtual host is URL-encoded in the deployed connection URI. Standard local RabbitMQ uses its default virtual host.

The management URL is available as `rabbit.GetRailwayRabbitMQManagementUrl()` after deployment; it contains no credentials. Core Railway outputs expose broker identities and its private hostname. Username/password values and management tokens must never appear in checked-in handoff evidence.

## Repository verification

```powershell
pwsh ./eng/Prepare-RailwayDependency.ps1
dotnet test Aspire.Hosting.RabbitMQ.Railway.slnx -c Release
pwsh ./eng/Test-AspireVersionPins.ps1
pwsh ./eng/Validate-TypeScriptAppHostPackage.ps1
```

The live pilot uses an external temporary consumer restored from packed NuGet packages. See [the live handoff](docs/live-verification.md) for deployed resource identities, tests, and recovery observations.

## Release setup

Configure repository secret `NUGET_USER` and a NuGet.org trusted publishing policy for this repository's `publish.yml` workflow. NuGet OIDC obtains a temporary key; no permanent NuGet API key is needed. Blacksmith runner access must be available to the repository. Standard checks need no Railway secrets. Optional live verification needs a dedicated Railway project/environment and scoped deployment credentials. Publish the shared Railway package before this package.
