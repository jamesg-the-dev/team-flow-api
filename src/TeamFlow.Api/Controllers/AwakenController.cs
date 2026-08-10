using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Api.Controllers;

[ApiController]
public class AwakenController(TeamFlowDbContext db) : ControllerBase
{
    [HttpGet]
    [Route("wakeup")]
    public async Task<IActionResult> Get()
    {
        await db.Profiles.FirstOrDefaultAsync();
        return Ok(new { awake = true });
    }
}
