using CloudsferQA.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudsferQA.Controllers;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly StatsService _stats;

    public ApiController(StatsService stats) => _stats = stats;

    [HttpGet("stats/{sessionId:int}")]
    public async Task<IActionResult> Stats(int sessionId)
    {
        var dto = await _stats.GetStatsAsync(sessionId);
        if (dto == null) return NotFound(new { error = "Session not found." });
        return Ok(dto);
    }
}
