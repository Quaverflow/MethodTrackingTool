namespace MethodTrackerTool.MockApi;

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
    public async Task Call() => await Task.CompletedTask;
}
