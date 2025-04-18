using MethodTrackerTool;
using MethodTrackerTool.TestApi;
using Microsoft.AspNetCore.TestHost;

namespace MethodTracker.Tests.ConcurrentTests.Api;

public class ApiTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public ApiTests()
    {
        _server = new TestServer(new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureServices(x => x.AddMethodLoggerFilter()));
        _client = _server.CreateClient();
    }

    public async Task Get_Endpoint_ReturnsSuccessStatusCode(string test) =>
        await MethodLogger.InitializeAsync(test, async () =>
        {
            var response = await _client.GetAsync($"/WeatherForecast/{test}");
            response.EnsureSuccessStatusCode();
        });

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }
}