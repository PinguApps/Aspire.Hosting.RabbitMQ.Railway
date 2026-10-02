# C# AppHost

Install `PinguApps.Aspire.Hosting.RabbitMQ.Railway` 1.0.0 in a .NET 10 AppHost using Aspire 13.6.0. The package includes the standard RabbitMQ integration and shared Railway publisher.

Create a Railway target from recorded project/environment/site identifiers and an environment-scoped project token. Pass explicit application username/password parameters to `AddRabbitMQ`, then call `PublishToRailway(target, operatorUser, operatorPassword, configure)`.

Create the target and deployment parameters inside `builder.ExecutionContext.IsPublishMode`. Keep the broker and consuming workloads outside the branch; optional publish-only credentials become `null` for standard local defaults. The compile-checked snippet demonstrates this boundary.

Both passwords must be distinct secret parameters with at least 32 characters. Retain them across deployments. Use `.WithReference(rabbit)` on consuming workloads to receive the private application connection string. Configure their retained images through the shared publisher.

See the [compile-checked snippet](../samples/AppHostSnippets/RailwayRabbitMQAppHostSnippets.cs) and [README example](../README.md).
