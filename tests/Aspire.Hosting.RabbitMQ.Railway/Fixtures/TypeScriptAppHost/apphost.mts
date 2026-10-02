import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();
const project = await builder.addParameter("railway-project-id");
const environment = await builder.addParameter("railway-environment-id");
const token = await builder.addParameter("railway-api-token", { secret: true });
const siteKey = await builder.addParameter("site-key");
const target = await builder.addRailwayTarget("railway", project, environment, token, siteKey);
const user = await builder.addParameter("rabbitmq-application-user");
const password = await builder.addParameter("rabbitmq-application-password", { secret: true });
const operator = await builder.addParameter("rabbitmq-operator-user");
const operatorPassword = await builder.addParameter("rabbitmq-operator-password", { secret: true });
let broker = await builder.addRabbitMQ("rabbitmq", { userName: user, password });
broker = await broker.withManagementPlugin();
broker = await broker.publishToRailway(target, operator, operatorPassword, {
  serviceName: "site-rabbitmq",
  proxyServiceName: "site-rabbitmq-management",
  region: "europe-west4-drams3a",
  memoryGB: 1,
  vCpus: 1,
  virtualHost: "site",
  configurePermissions: "^site\\.",
  writePermissions: "^(site\\.|amq\\.default$)",
  readPermissions: "^site\\.",
});
void broker;
const managementUrl = await broker.getRailwayRabbitMQManagementUrl();
void managementUrl;
const app = await builder.build();
await app.run();
