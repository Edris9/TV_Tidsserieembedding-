using Microsoft.AspNetCore.Mvc;
using TvTidsserieembedding.Application.Services;
using TvTidsserieembedding.Domain.Interfaces;
using TvTidsserieembedding.Infrastructure.TrafikverketApi;

namespace TvTidsserieembedding.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly SensorAnalysisService _service;
    private readonly TrafikverketClient _trafikverket;
    private readonly IEmbeddingService _embedding;
    private readonly IAnomalyDetector _detector;

    public SensorController(
        SensorAnalysisService service,
        TrafikverketClient trafikverket,
        IEmbeddingService embedding,
        IAnomalyDetector detector)
    {
        _service = service;
        _trafikverket = trafikverket;
        _embedding = embedding;
        _detector = detector;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWeather()
    {
        var data = await _trafikverket.GetWeatherDataAsync();
        return Content(data, "application/json");
    }

    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings()
    {
        var raw = await _trafikverket.GetWeatherDataAsync();
        var readings = WeatherResponseParser.Parse(raw);
        return Ok(readings);
    }

    [HttpGet("analyze-live")]
    public async Task<IActionResult> AnalyzeLive()
    {
        var raw = await _trafikverket.GetWeatherDataAsync();
        var readings = WeatherResponseParser.Parse(raw).ToList();

        var embedding = await _embedding.EmbedAsync(readings);
        var isAnomaly = await _detector.IsAnomalyAsync(embedding);

        return Ok(new
        {
            stationCount = readings.Count,
            embedding = embedding.Take(5),
            isAnomaly
        });
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
