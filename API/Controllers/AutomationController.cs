using Microsoft.AspNetCore.Mvc;

namespace NexusHome.IoT.API.Controllers
{
    [ApiController]
    [Route("api/automation/rules")]
    public class AutomationController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll() => Ok(Array.Empty<object>());

        [HttpPost]
        public IActionResult Create([FromBody] object body) => Created("/api/automation/rules/1", new { id = 1 });

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] object body) => Ok(new { id });

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id) => NoContent();
    }
}
