# Live verification

Dedicated project: `PIN-646 RabbitMQ Integration` (`3baaaee7-2fe4-43f1-868e-a3c3408fa7f9`). Production environment: `dccd9ec5-6ef2-4bcc-b584-e4fafd457600`. Allocation key: `pin646-rabbitmq-integration`.

Verified on 2 October 2026 using external temporary C# AppHosts restored from packed NuGet packages, then real `aspire deploy` with Aspire CLI 13.6.0. Credentials and generated deployment state remain outside this repository.

## Retained pilot

| Resource | Identity | Inspection |
| --- | --- | --- |
| Broker | `a901bb5a-3431-42c4-8635-3c5dae876725` | Private `rabbitmq-broker.railway.internal:5672`; no public domain or TCP proxy. |
| Broker volume | `9ea0966a-d484-4b16-bbbd-c42cc6eb1a37` | `/var/lib/rabbitmq`, 5 GB, SFO. |
| Management proxy | `a2391901-00e1-4818-9553-3b8f976ec0bc` | [Authenticated HTTPS management](https://rabbitmq-management-production-f450.up.railway.app). |
| Private AMQP probe | `423d04dc-4230-4234-9f24-0ad5e216e27f` | [Readiness](https://amqp-probe-production.up.railway.app/health). |

All three services reached Railway `SUCCESS` and remain running for inspection. The probe is a dedicated integration-test artifact, outside the shipped package. It exposes only operations against its test queue; it must not be reused as a production application.

## Completed checks

- Standard local resource preservation, publishing contract, credential validation, marker-collision rejection, local management URL, DTO parity, pinned image defaults, proxy routing, and credential-gated project-token scope: nine offline tests and one live scope test passed.
- Clean pinned-source dependency build and NuGet-backed TypeScript SDK restore/typecheck succeeded. Both pipeline listings contained broker and dependent management proxy steps.
- Real private AMQP connections resolved both private IPv4 and IPv6 addresses. A password containing URI punctuation and virtual host `site /integration` connected successfully through the redirected application connection string.
- Publisher confirms, a durable `site.persistence` queue, and persistent messages worked. Message body survived a real broker redeployment and was consumed with a manual acknowledgement.
- Application attempts to create `unrelated.queue` received AMQP 403. `guest:guest` authentication was rejected.
- Management API overview: operator HTTP 200, application and anonymous HTTP 401. Anonymous/application attempts to create a virtual host received 401; the attempted virtual host remained absent.
- No `guest` account existed. A deliberately inserted, unprivileged guest fixture on the retained volume was removed by the next broker startup, verified with management HTTP 404.
- Railway variable readback could not retrieve plaintext for either broker password or inherited `RABBITMQ_DEFAULT_PASS`. The probe's connection string, URI, and password variables were sealed separately through core configuration.
- An unchanged deploy reused all three exact deployment IDs. Secret rotation with explicit current-value parameters preserved service and volume identities: new operator credentials returned 200, former credentials returned 401, and the consumer connected with the rotated application password.
- `ExistingOnly` plus recorded broker/proxy/probe IDs adopted the same resources from a fresh external AppHost. Deliberately removing broker/proxy ownership markers then redeploying with those explicit IDs restored the markers and retained the volume.
- Updating broker memory from 1 GB to 1.25 GB retained service and volume identities. Requesting AMS for the existing SFO volume failed during preflight with the specific volume-region migration error, before deployment writes.

## Recovery and limits

Retain the project/environment/site IDs, volume, independently generated credentials, and protected Aspire deployment state. Recover previously unmarked infrastructure only with explicit service IDs after checking its ownership and users. Never clear the whole deployment cache as a routine secret-rotation step. See [deployment behaviour](deployment-behaviour.md) for the explicit-value parameter overload.

The pilot uses SFO because its original persistent volume was allocated there. Moving that volume requires an operator data migration. Custom management domains require user-owned DNS and were contract-tested rather than provisioned in this pilot. Existing operator-created users/topology remain on an adopted volume except `guest`; audit them. Single-node persistence is not high availability.
