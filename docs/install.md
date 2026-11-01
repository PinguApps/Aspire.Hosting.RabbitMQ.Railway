# Installation

Use .NET 10, Aspire CLI 13.6.4, and matching built-in integration pins:

```json
{
  "packages": {
    "Aspire.Hosting.RabbitMQ": "13.6.4",
    "PinguApps.Aspire.Hosting.Railway": "1.0.0",
    "PinguApps.Aspire.Hosting.RabbitMQ.Railway": "1.0.0"
  }
}
```

The shared Railway package 1.0.0 must be published first. See [README](../README.md) for C#, TypeScript, security, deployment, and release setup.
