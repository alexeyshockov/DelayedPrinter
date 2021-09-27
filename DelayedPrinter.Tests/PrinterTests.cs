using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DelayedPrinter.Tests
{
    public class PrinterTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        public const string Url = "/Print";

        private readonly WebApplicationFactory<Startup> _factory;

        private TerminalStub _terminal = new();

        public PrinterTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        private HttpClient CreateAppClient() =>
            _factory.WithWebHostBuilder(ConfigureWebHostBuilder).CreateClient();

        private void ConfigureWebHostBuilder(IWebHostBuilder whb) => whb
            .UseEnvironment(Environments.Development)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ITerminal>(_ => _terminal);
            });

        [Fact]
        public async Task Print_MessageInThePast_PrintsImmediately()
        {
            // Arrange
            var client = CreateAppClient();
            var message = "Test";

            // Act
            var response = await client.PostAsJsonAsync(Url, new
            {
                message = message,
                printAt = DateTimeOffset.Now,
            });

            // Assert
            response.EnsureSuccessStatusCode();

            await Task.Delay(50); // Just give it a bit of milliseconds to handle everything
            _terminal.PrintedMessages.Should().Contain(message);
        }

        [Fact]
        public async Task Print_MessageInTheFuture_PrintsDelayed()
        {
            // Arrange
            var client = CreateAppClient();
            var message = "Test";

            // Act
            var response = await client.PostAsJsonAsync(Url, new
            {
                message = message,
                printAt = DateTimeOffset.Now.AddSeconds(5),
            });

            // Assert
            response.EnsureSuccessStatusCode();

            await Task.Delay(50); // Just give it a bit of milliseconds to handle everything
            _terminal.PrintedMessages.Should().BeEmpty();

            await Task.Delay(TimeSpan.FromSeconds(4));
            _terminal.PrintedMessages.Should().BeEmpty(); // Still empty

            await Task.Delay(TimeSpan.FromSeconds(1));
            _terminal.PrintedMessages.Should().Contain(message); // Got it!
        }
    }
}
