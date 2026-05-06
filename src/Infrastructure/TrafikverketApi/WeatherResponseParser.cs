using System.Text.Json;
using TvTidsserieembedding.Domain.Entities;

namespace TvTidsserieembedding.Infrastructure.TrafikverketApi;

public readonly record struct WeatherParseStats(
    IReadOnlyList<SensorReading> Readings,
    int MeasurepointsInResponse,
    int SkippedIncomplete);

public static class WeatherResponseParser
{
    public static IEnumerable<SensorReading> Parse(string json) =>
        ParseWithStats(json).Readings;

    /// <summary>
    /// Parsar JSON och räknar hur många <c>WeatherMeasurepoint</c> som fanns i svaret respektive hoppades över
    /// (saknar id, provtid eller lufttemperatur).
    /// </summary>
    public static WeatherParseStats ParseWithStats(string json)
    {
        var readings = new List<SensorReading>();
        if (string.IsNullOrWhiteSpace(json))
            return new WeatherParseStats(readings, 0, 0);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("RESPONSE", out var response))
            return new WeatherParseStats(readings, 0, 0);
        if (!response.TryGetProperty("RESULT", out var resultArr) || resultArr.GetArrayLength() == 0)
            return new WeatherParseStats(readings, 0, 0);

        var first = resultArr[0];
        if (!first.TryGetProperty("WeatherMeasurepoint", out var measurepoints))
            return new WeatherParseStats(readings, 0, 0);
        if (measurepoints.ValueKind != JsonValueKind.Array)
            return new WeatherParseStats(readings, 0, 0);

        var total = measurepoints.GetArrayLength();
        var skipped = 0;

        foreach (var point in measurepoints.EnumerateArray())
        {
            if (!point.TryGetProperty("Id", out var idEl))
            {
                skipped++;
                continue;
            }

            var id = idEl.GetString() ?? "unknown";
            if (!point.TryGetProperty("Observation", out var obs))
            {
                skipped++;
                continue;
            }

            if (!obs.TryGetProperty("Sample", out var sampleEl))
            {
                skipped++;
                continue;
            }

            var sample = sampleEl.GetString();
            if (string.IsNullOrEmpty(sample))
            {
                skipped++;
                continue;
            }

            if (!obs.TryGetProperty("Air", out var air)
                || !air.TryGetProperty("Temperature", out var tempEl)
                || !tempEl.TryGetProperty("Value", out var valueEl))
            {
                skipped++;
                continue;
            }

            var timestamp = DateTime.Parse(sample);
            var airTemp = valueEl.GetDouble();

            readings.Add(new SensorReading
            {
                SensorId = id,
                Timestamp = timestamp,
                Value = airTemp,
                Unit = "°C"
            });
        }

        return new WeatherParseStats(readings, total, skipped);
    }
}
