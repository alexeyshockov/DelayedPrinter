using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DelayedPrinter
{
    public interface IPrintScheduler
    {
        Task Schedule(DelayedPrintRequest request);
    }

    public class PrintScheduler : BackgroundService, IPrintScheduler
    {
        public const string RedisKey = "delayed";

        private readonly ILogger<PrintScheduler> _logger;
        private readonly IConnectionMultiplexer _connection;

        private readonly RedisValue _token = Guid.NewGuid().ToString();
        private readonly TimeSpan _lockTime = TimeSpan.FromSeconds(1);

        public PrintScheduler(ILogger<PrintScheduler> logger, IConnectionMultiplexer connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public async Task Schedule(DelayedPrintRequest request)
        {
            var message = JsonSerializer.Serialize(request);
            await _connection.GetDatabase().SortedSetAddAsync("delayed", message, request.Timestamp);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rand = new Random();
            while (!stoppingToken.IsCancellationRequested)
            {
                var messagesProcessed = await ScheduleMessages(_connection.GetDatabase());
                if (messagesProcessed == 0)
                {
                    _logger.LogDebug("Waiting...");

                    // Wait a random period of time until asking again
                    await Task.Delay(rand.Next(5, 50), stoppingToken);
                }
            }
        }

        private async Task<int> ScheduleMessages(IDatabase redis)
        {
            if (!redis.LockTake("scheduler", _token, _lockTime))
                return 0;

            try {
                // Request for all the time up until now...
                var messages = redis.SortedSetRangeByScore(RedisKey,
                    stop: DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // Up until now
                    take: 100); // Max amount of messages for one shot
                var messagesFetched = messages.Length;

                if (messagesFetched > 0)
                {
                    var moveTransaction = redis.CreateTransaction();

                    // Do not await operations in a transaction, they will be executed only on the commit moment
                    moveTransaction.SortedSetRemoveAsync(RedisKey, messages);
                    foreach (var message in messages)
                        moveTransaction.PublishAsync(Printer.RedisChannel, message);

                    var movedSuccessfully = await moveTransaction.ExecuteAsync();

                    if (movedSuccessfully)
                        _logger.LogDebug("Messages published for processing: {Amount}", messagesFetched);
                    else
                        _logger.LogError("Could not publish messages for processing");
                }

                return messagesFetched;
            } finally {
                redis.LockRelease("scheduler", _token);
            }
        }
    }
}
