using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Infrastructure.Messaging;

public static class RabbitMqConnectionFactory
{
    public static async Task<IConnection> CreateAsync(IOptions<RabbitMqOptions> options, CancellationToken ct = default)
    {
        var o = options.Value;
        var factory = new ConnectionFactory
        {
            Uri = new Uri(o.ConnectionString)
        };
        return await factory.CreateConnectionAsync(ct);
    }
}
