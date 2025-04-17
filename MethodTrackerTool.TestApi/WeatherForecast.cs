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
    Task Call();
}

public class WeatherForecastService : IWeatherForecastService
{
    public async Task Call() => await new OrderService("ApiCall", DateTime.Now).ProcessOrderAsync(new OrderRequest
    {
        UserId = 13,
        ProductIds = [1, 4, 55, 342, 33, 334, 864, 268, 1042],
        TotalAmount = 20
    });
}
