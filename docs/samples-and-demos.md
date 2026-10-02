# Samples and demos

- [C# snippet](../samples/AppHostSnippets/RailwayRabbitMQAppHostSnippets.cs): compile-checked target, parameters, and broker publishing.
- [TypeScript AppHost](../samples/TypeScriptAppHost/apphost.mts): generated bindings and DTO configuration.
- [Live verification](live-verification.md): pilot resources, security checks, and durable message recovery.

The real deployment consumer lives outside this repository and references packed NuGet packages. Its probe is an integration-test service rather than part of the shipped package. Credentials and generated artifacts are excluded from version control.
