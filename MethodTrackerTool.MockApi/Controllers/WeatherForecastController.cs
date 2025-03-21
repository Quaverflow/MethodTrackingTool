using Microsoft.AspNetCore.Mvc;

namespace MethodTrackerTool.MockApi.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastService service) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult> Get()
    {
        await service.Call();

        return Accepted();
    }
}
