using Microsoft.AspNetCore.Mvc;

namespace TeamFlow.Api.Controllers;

[ApiController]
public class AwakenController : ControllerBase
{
    [HttpGet]
    [Route("wakeup")]
    public IActionResult Wakeup() => Ok(new { awake = true });
}
