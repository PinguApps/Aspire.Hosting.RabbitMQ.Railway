# Installation

The package targets .NET 10 and requires Aspire 17.0.0 or later. The examples use Aspire CLI 17.0.0 with matching built-in integration versions:

```json
{
  "packages": {
    "Aspire.Hosting.RabbitMQ": "17.0.0",
    "PinguApps.Aspire.Hosting.Railway": "1.1.7",
    "PinguApps.Aspire.Hosting.RabbitMQ.Railway": "1.0.5"
  }
}
```

The shared Railway package is restored from NuGet.org. See [README](../README.md) for C#, TypeScript, security, deployment, and release setup.
