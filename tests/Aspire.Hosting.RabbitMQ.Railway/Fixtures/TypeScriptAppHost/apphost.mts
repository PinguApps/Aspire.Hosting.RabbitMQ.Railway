import { createBuilder } from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();
const publishing = await (await builder.executionContext()).isPublishMode();
const user = publishing ? await builder.addParameter("rabbitmq-application-user") : undefined;
const password = publishing ? await builder.addParameter("rabbitmq-application-password", { secret: true }) : undefined;
let broker = await builder.addRabbitMQ("rabbitmq", { userName: user, password });
broker = await broker.withManagementPlugin();

if (publishing) {
  const project = await builder.addParameter("railway-project-id");
  const environment = await builder.addParameter("railway-environment-id");
  const token = await builder.addParameter("railway-api-token", { secret: true });
  const siteKey = await builder.addParameter("site-key");
  const target = await builder.addRailwayTarget("railway", project, environment, token, siteKey);
  const operator = await builder.addParameter("rabbitmq-operator-user");
  const operatorPassword = await builder.addParameter("rabbitmq-operator-password", { secret: true });
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
}

const managementUrl = await broker.getRailwayRabbitMQManagementUrl();
void managementUrl;
const app = await builder.build();
await app.run();
