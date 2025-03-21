using MethodTrackerTool;
using MethodTrackerTool.TestApi;
using Microsoft.AspNetCore.TestHost;

// Replace with your actual project's namespace

namespace MethodTracker.Tests
{
    public class ApiIntegrationTests : IDisposable
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public ApiIntegrationTests()
        {
            // Arrange: Create a new TestServer using the Startup class.
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            _client = _server.CreateClient();
        }

        [Fact]
        public async Task Get_Endpoint_ReturnsSuccessStatusCode()
        {
            MethodLogger.Initialize("Get_Endpoint_ReturnsSuccessStatusCode");
            var response = await _client.GetAsync("/WeatherForecast"); // Adjust the URL as needed.
            response.EnsureSuccessStatusCode();
            MethodLogger.PrintJson();
        }

        public void Dispose()
        {
            // Clean up resources.
            _client.Dispose();
            _server.Dispose();
        }
    }
}