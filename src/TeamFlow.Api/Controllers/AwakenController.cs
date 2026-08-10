using Microsoft.AspNetCore.Mvc;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Api.Controllers;

[ApiController]
public class AwakenController(TeamFlowDbContext db) : ControllerBase
{
    [HttpGet]
    [Route("wakeup")]
    public async Task<IActionResult> Get()
    {
        await db.Database.CanConnectAsync();
        return Ok(new { awake = true });
    }
}
