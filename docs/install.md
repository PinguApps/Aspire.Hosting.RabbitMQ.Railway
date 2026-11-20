# Installation

Use .NET 10, Aspire CLI 17.0.0, and matching built-in integration pins:

```json
{
  "packages": {
    "Aspire.Hosting.RabbitMQ": "17.0.0",
    "PinguApps.Aspire.Hosting.Railway": "1.1.5",
    "PinguApps.Aspire.Hosting.RabbitMQ.Railway": "1.0.5"
  }
}
```

The shared Railway package is restored from NuGet.org. See [README](../README.md) for C#, TypeScript, security, deployment, and release setup.
