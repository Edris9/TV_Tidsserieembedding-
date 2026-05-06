using Microsoft.AspNetCore.Mvc;
using TvTidsserieembedding.Application.Services;

namespace TvTidsserieembedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly SensorAnalysisService _service;

    public SensorController(SensorAnalysisService service)
    {
        _service = service;
    }

    [HttpGet("analyze")]
    public async Task<IActionResult> Analyze(
        [FromQuery] string sensorId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var isAnomaly = await _service.AnalyzeAsync(sensorId, from, to);
        return Ok(new { sensorId, isAnomaly });
    }
}
