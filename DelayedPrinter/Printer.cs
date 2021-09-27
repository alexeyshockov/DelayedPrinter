using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DelayedPrinter
{
    public class Printer : IHostedService
    {
        public const string RedisChannel = "messages";

        private readonly ILogger<Printer> _logger;

        private readonly IConnectionMultiplexer _connection;
        private readonly ITerminal _terminal;

        private ChannelMessageQueue _channel;

        public Printer(ILogger<Printer> logger, IConnectionMultiplexer connection, ITerminal terminal)
        {
            _logger = logger;
            _connection = connection;
            _terminal = terminal;
        }

        private void PrintMessage(ChannelMessage message)
        {
            try
            {
                var printRequest = JsonSerializer.Deserialize<DelayedPrintRequest>(message.Message);
                if (printRequest == null)
                    throw new Exception("Deserialization failed");

                _terminal.WriteLine(printRequest.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while handling a message");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Process messages one by one, preserving the order
            _channel = await _connection.GetSubscriber().SubscribeAsync("messages");
            _channel.OnMessage(PrintMessage);
        }

        public Task StopAsync(CancellationToken cancellationToken) => _channel.UnsubscribeAsync();
    }
}
