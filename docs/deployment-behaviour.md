# Deployment behaviour

`aspire deploy` delegates control-plane access and persisted identities to the shared Railway publisher. The pre-created environment must carry the shared `PINGUAPPS_SITE_KEY` allocation marker. A scoped project token must match the configured project/environment.

The broker uses a digest-pinned image and persistent volume. Startup validates credentials, hashes passwords, imports definitions, waits for RabbitMQ startup, and removes a retained `guest` account. The proxy resolves private DNS dynamically so broker redeployments can change private addresses safely.

Repeated deployments reuse service and volume identities. Changed secret fingerprints trigger redeployment without reading sealed values. Volume/region/public-exposure drift requiring destructive reconciliation fails for operator attention. No services or volumes are deleted.

Aspire caches parameter values in protected deployment state. For rotation, use `AddParameter(name, currentSecretValue, secret: true)` with the current value from your secret store; that explicit-value overload updates a same-name parameter despite cached values. TypeScript exposes `addParameter(name, { value: currentSecretValue, secret: true })`. Changing an AppHost configuration file alone can retain prior values. Preserve publisher identity and fingerprint state instead of clearing the complete deployment cache.
