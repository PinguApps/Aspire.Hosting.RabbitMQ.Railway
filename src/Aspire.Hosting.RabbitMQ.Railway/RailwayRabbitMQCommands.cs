using System.Text;
using System.Text.Json;

namespace Aspire.Hosting.RabbitMQ.Railway;

internal static class RailwayRabbitMQCommands
{
    internal static string Broker(RailwayRabbitMQDeploymentOptions options)
    {
        string definitions = JsonSerializer.Serialize(new
        {
            users = new[]
            {
                new { name = "PAPP_OPERATOR_USER", password_hash = "PAPP_OPERATOR_HASH", hashing_algorithm = "rabbit_password_hashing_sha256", tags = "administrator" },
                new { name = "PAPP_APPLICATION_USER", password_hash = "PAPP_APPLICATION_HASH", hashing_algorithm = "rabbit_password_hashing_sha256", tags = string.Empty },
            },
            vhosts = new[] { new { name = options.VirtualHost } },
            permissions = new[]
            {
                new { user = "PAPP_OPERATOR_USER", vhost = options.VirtualHost, configure = ".*", write = ".*", read = ".*" },
                new { user = "PAPP_APPLICATION_USER", vhost = options.VirtualHost, configure = options.ConfigurePermissions, write = options.WritePermissions, read = options.ReadPermissions },
            },
        });
        string encodedDefinitions = Convert.ToBase64String(Encoding.UTF8.GetBytes(definitions));
        string script = $$"""
            set -eu
            for username in "$PAPP_RABBITMQ_OPERATOR_USER" "$PAPP_RABBITMQ_APPLICATION_USER"; do
              case "$username" in ''|guest|*PAPP_*|*[!a-zA-Z0-9_.-]*) echo 'RabbitMQ usernames must be non-guest identifiers without reserved PAPP_ definition markers.' >&2; exit 64;; esac
            done
            [ "$PAPP_RABBITMQ_OPERATOR_USER" != "$PAPP_RABBITMQ_APPLICATION_USER" ] || { echo 'RabbitMQ operator and application usernames must differ.' >&2; exit 64; }
            [ "${#PAPP_RABBITMQ_OPERATOR_PASSWORD}" -ge 32 ] || { echo 'RabbitMQ operator password must contain at least 32 characters.' >&2; exit 64; }
            [ "${#PAPP_RABBITMQ_APPLICATION_PASSWORD}" -ge 32 ] || { echo 'RabbitMQ application password must contain at least 32 characters.' >&2; exit 64; }
            [ "$PAPP_RABBITMQ_OPERATOR_PASSWORD" != "$PAPP_RABBITMQ_APPLICATION_PASSWORD" ] || { echo 'RabbitMQ operator and application passwords must differ.' >&2; exit 64; }
            operator_hash=$(rabbitmqctl -q hash_password "$PAPP_RABBITMQ_OPERATOR_PASSWORD")
            application_hash=$(rabbitmqctl -q hash_password "$PAPP_RABBITMQ_APPLICATION_PASSWORD")
            printf '%s' '{{encodedDefinitions}}' | base64 -d | sed -e "s/PAPP_OPERATOR_USER/$PAPP_RABBITMQ_OPERATOR_USER/g" -e "s/PAPP_APPLICATION_USER/$PAPP_RABBITMQ_APPLICATION_USER/g" -e "s@PAPP_OPERATOR_HASH@$operator_hash@g" -e "s@PAPP_APPLICATION_HASH@$application_hash@g" > /etc/rabbitmq/definitions.json
            printf '%s\n' 'management.load_definitions = /etc/rabbitmq/definitions.json' 'listeners.tcp.default = 5672' 'management.tcp.port = 15672' 'loopback_users.guest = true' > /etc/rabbitmq/conf.d/99-railway.conf
            unset RABBITMQ_DEFAULT_USER RABBITMQ_DEFAULT_PASS
            docker-entrypoint.sh rabbitmq-server &
            server_pid=$!
            trap 'kill -TERM "$server_pid"; wait "$server_pid"' TERM INT
            attempts=0
            until gosu rabbitmq rabbitmq-diagnostics -q ping >/dev/null 2>&1; do
              attempts=$((attempts + 1))
              if [ "$attempts" -ge 60 ]; then kill -TERM "$server_pid"; exit 70; fi
              sleep 2
            done
            gosu rabbitmq rabbitmqctl -q await_startup --timeout 120
            if gosu rabbitmq rabbitmqctl -q list_users | awk '{print $1}' | grep -qx guest; then
              gosu rabbitmq rabbitmqctl -q delete_user guest
            fi
            wait "$server_pid"
            """;
        return ShellCommand(script);
    }

    internal static string Proxy()
    {
        string script = """
            set -eu
            case "$PAPP_RABBITMQ_HOST" in *[!a-zA-Z0-9.-]*|'') exit 64;; esac
            dns_server=$(awk '/^nameserver/{print $2; exit}' /etc/resolv.conf)
            case "$dns_server" in *:*) dns_server="[$dns_server]";; esac
            [ -n "$dns_server" ] || exit 64
            cat > /etc/nginx/conf.d/default.conf <<EOF
            server {
              listen 8080;
              listen [::]:8080;
              server_tokens off;
              resolver $dns_server valid=5s;
              resolver_timeout 5s;
              set \$rabbitmq_upstream "$PAPP_RABBITMQ_HOST:15672";
              location = /health { access_log off; proxy_pass http://\$rabbitmq_upstream/; }
              location / {
                proxy_pass http://\$rabbitmq_upstream;
                proxy_http_version 1.1;
                proxy_set_header Host \$host;
                proxy_set_header X-Forwarded-Proto https;
                proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
                proxy_read_timeout 60s;
                proxy_connect_timeout 5s;
              }
            }
            EOF
            exec nginx -g 'daemon off;'
            """;
        return ShellCommand(script);
    }

    private static string ShellCommand(string script)
    {
        return "/bin/sh -ec '" + script.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }
}
