using Microsoft.AspNetCore.Mvc;

namespace NexusHome.IoT.API.Controllers
{
    [ApiController]
    [Route("api/energy")]
    public class EnergyController : ControllerBase
    {
        [HttpGet("consumption")]
        public IActionResult Consumption() => Ok(new { totalKwh = 0m });

        [HttpGet("cost")]
        public IActionResult Cost() => Ok(new { totalCost = 0m });

        [HttpGet("forecast")]
        public IActionResult Forecast() => Ok(new { hours = 24, series = Array.Empty<object>() });
    }
}
