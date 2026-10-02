# TypeScript AppHost

List `Aspire.Hosting.RabbitMQ` 13.6.0, `PinguApps.Aspire.Hosting.Railway` 1.0.0, and `PinguApps.Aspire.Hosting.RabbitMQ.Railway` 1.0.0 explicitly in `aspire.config.json`, then run `aspire restore`. The shared target's generated exports require the explicit core package entry.

The generated builder exposes `publishToRailway(target, operatorUser, operatorPassword, options)`. The DTO supports the same settings as the C# callback. No callbacks cross the guest-language boundary.

The [sample AppHost](../samples/TypeScriptAppHost/apphost.mts) demonstrates parameter wiring. The package gate packs the actual NuGet package, restores its generated SDK in an isolated fixture, typechecks the program, and inspects both publish and deploy pipelines without Railway mutations.
