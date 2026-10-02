# Configuration

`RailwayRabbitMQDeploymentOptions` and its TypeScript DTO expose:

| Setting | Behaviour |
| --- | --- |
| `Image`, `ProxyImage` | Immutable digest references for RabbitMQ and Nginx. |
| `ServiceName`, `ProxyServiceName` | Stable Railway service names. |
| `Region` | Region for both services; existing volumes cannot be silently moved. |
| `MemoryGB`, `VCpus` | Broker resource limits. |
| `VolumeMountPath` | Must remain `/var/lib/rabbitmq`. |
| `VirtualHost` | Application virtual host; URI-encoded in the connection string. |
| `ConfigurePermissions`, `WritePermissions`, `ReadPermissions` | Application permission regexes; defaults permit `site.*` topology and default-exchange publication. |
| `ManagementDomain` | Optional custom domain; configure DNS separately. |
| `OwnershipMode` | Shared publisher ownership policy for both services. |
| `ExistingServiceId`, `ExistingProxyServiceId` | Explicit identities for previously unmarked infrastructure adoption. |

Application users have no management tags. The operator has administrator management permissions and unrestricted permissions on the selected virtual host. Local development retains the standard broker's default virtual host.
