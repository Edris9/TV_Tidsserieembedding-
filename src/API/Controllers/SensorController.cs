using Microsoft.AspNetCore.Mvc;
using TvTidsserieembedding.API.Models;
using TvTidsserieembedding.Application.Services;
using TvTidsserieembedding.Domain.Entities;
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
    public async Task<IActionResult> GetWeather([FromQuery] int limit = 2000)
    {
        var data = await _trafikverket.GetWeatherDataAsync(limit);
        return Content(data, "application/json");
    }

    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings([FromQuery] int limit = 2000)
    {
        var last = SwedishMonthBounds.GetCalendarMonthMonthsAgo(1);

        var rawCurrent = await _trafikverket.GetWeatherDataAsync(limit);
        var rawLastMonth = await _trafikverket.GetWeatherDataAsync(
            limit,
            last.FromInclusive,
            last.ToInclusive);

        var statsCurrent = WeatherResponseParser.ParseWithStats(rawCurrent);
        var current = LatestPerStation(statsCurrent.Readings);
        var lastMonth = LatestPerStation(WeatherResponseParser.Parse(rawLastMonth));

        var rows = new List<StationComparisonRowDto>();
        foreach (var kv in current)
        {
            rows.Add(
                new StationComparisonRowDto
                {
                    SensorId = kv.Key,
                    Current = ToSnapshot(kv.Value),
                    LastMonth = lastMonth.TryGetValue(kv.Key, out var lr) ? ToSnapshot(lr) : null,
                });
        }

        return Ok(
            new ReadingsComparisonResponseDto
            {
                LastMonthHeading = last.LabelSv,
                Rows = rows,
                RequestedLimit = limit,
                MeasurepointsInResponse = statsCurrent.MeasurepointsInResponse,
                SkippedIncompleteMeasurepoints = statsCurrent.SkippedIncomplete,
                ParsedObservationCount = statsCurrent.Readings.Count,
            });
    }

    private static Dictionary<string, SensorReading> LatestPerStation(
        IEnumerable<SensorReading> items)
    {
        return items
            .GroupBy(r => r.SensorId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Timestamp).First());
    }

    private static ReadingSnapshotDto ToSnapshot(SensorReading r) =>
        new()
        {
            Timestamp = r.Timestamp,
            Value = r.Value,
            Unit = r.Unit,
        };

    /// <summary>
    /// Utgår från nuvarande stationsvärden och genererar 30 fejkade historiska punkter per station
    /// med slumpmässig avvikelse ±3 °C; tidsstämplar fördelas över föregående kalendermånad (svensk tid).
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 2000)
    {
        var raw = await _trafikverket.GetWeatherDataAsync(limit);
        var baseline = LatestPerStation(WeatherResponseParser.Parse(raw));
        var rnd = Random.Shared;
        var list = new List<FakeHistoryPointDto>(baseline.Count * 30);

        foreach (var kv in baseline)
        {
            var baseTemp = kv.Value.Value;
            for (var i = 0; i < 30; i++)
            {
                var ts = SwedishMonthBounds.RandomUtcInSwedishCalendarMonthMonthsAgo(1, rnd);
                var delta = rnd.NextDouble() * 6.0 - 3.0;
                var value = baseTemp + delta;
                list.Add(
                    new FakeHistoryPointDto
                    {
                        SensorId = kv.Key,
                        Timestamp = ts,
                        Value = Math.Round(value, 2),
                    });
            }
        }

        return Ok(list);
    }

    [HttpGet("analyze-live")]
    public async Task<IActionResult> AnalyzeLive([FromQuery] int limit = 2000)
    {
        var raw = await _trafikverket.GetWeatherDataAsync(limit);
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
