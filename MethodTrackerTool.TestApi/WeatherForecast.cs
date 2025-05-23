using MethodTracker.MockProject;

namespace MethodTrackerTool.TestApi;

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}

public interface IWeatherForecastService
{
    Task Call(string name);
}

public class WeatherForecastService : IWeatherForecastService
{
    public async Task Call(string name)
    {
        await new OrderService(name, DateTime.Now).ProcessOrderAsync(new OrderRequest
        {
            UserId = 13,
            ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
            TotalAmount = 20
        });

        Task.Run(Endless);
    }

    private void Endless() => Thread.Sleep(12000);
}