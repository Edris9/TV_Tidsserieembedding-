using System.Text.Json;
using TvTidsserieembedding.Domain.Entities;

namespace TvTidsserieembedding.Infrastructure.TrafikverketApi;

public static class WeatherResponseParser
{
    public static IEnumerable<SensorReading> Parse(string json)
    {
        var readings = new List<SensorReading>();

        var doc = JsonDocument.Parse(json);
        var measurepoints = doc
            .RootElement
            .GetProperty("RESPONSE")
            .GetProperty("RESULT")[0]
            .GetProperty("WeatherMeasurepoint");

        foreach (var point in measurepoints.EnumerateArray())
        {
            var id = point.GetProperty("Id").GetString() ?? "unknown";
            var sample = point
                .GetProperty("Observation")
                .GetProperty("Sample")
                .GetString();

            var timestamp = DateTime.Parse(sample!);

            var airTemp = point
                .GetProperty("Observation")
                .GetProperty("Air")
                .GetProperty("Temperature")
                .GetProperty("Value")
                .GetDouble();

            readings.Add(new SensorReading
            {
                SensorId = id,
                Timestamp = timestamp,
                Value = airTemp,
                Unit = "°C"
            });
        }

        return readings;
    }
}
