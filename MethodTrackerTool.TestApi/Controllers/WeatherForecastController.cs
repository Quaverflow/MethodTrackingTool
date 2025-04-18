using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace MethodTrackerTool.TestApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastService service) : ControllerBase
{
    [HttpGet]
    [Route(("{name}"))]
    public async Task<ActionResult> Get([FromRoute]string name)
    {
        await service.Call(name);

        return Accepted();
    }
}