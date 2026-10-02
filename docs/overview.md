# Overview

The package adds `PublishToRailway` to Aspire's standard `RabbitMQServerResource`. Local `aspire run` retains the standard RabbitMQ container. Publishing deploys a private broker with a persistent volume and a public HTTPS management proxy authenticated by RabbitMQ.

One broker belongs to one site/environment. The package creates a dedicated application virtual host, a scoped application account, and a distinct operator account. Retry policies, dead-letter topology, and message processing remain application concerns.

See [the complete API example](../README.md) and [verified live behaviour](live-verification.md).
