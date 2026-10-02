# Outputs and security

`.WithReference(rabbit)` resolves the main connection string to the private Railway hostname, port 5672, URL-encoded application credentials, and configured virtual host. Use that connection string for a non-default virtual host; the built-in component property `RABBIT_URI` retains Aspire's format without a virtual-host path.

`rabbit.GetRailwayRabbitMQManagementUrl()` returns the proxy's public HTTPS URL during publication and the local management endpoint during development. Call `WithManagementPlugin` for local access; missing local/publishing configuration produces an explicit error. The URL contains no credentials. Core outputs expose private hostname and service identity. AMQP and direct broker management remain private; no TCP proxy is created.

Broker password variables are sealed in Railway. Consumers should seal credential-bearing variables using core `SealedVariables`. For a resource named `rabbit`, those include `ConnectionStrings__rabbit`, `RABBIT_URI`, and `RABBIT_PASSWORD`. Protect deployment state and parameter stores because Aspire caches parameters locally.

Anonymous and application accounts cannot use management APIs. The operator is intentionally privileged. Existing operator-created users/topology on an adopted volume remain, except `guest`; audit them when adopting infrastructure. The package adds no separate identity provider.
