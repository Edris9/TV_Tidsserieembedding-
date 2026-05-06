namespace TvTidsserieembedding.API.Models;

public class ReadingSnapshotDto
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = "°C";
}

public class StationComparisonRowDto
{
    public string SensorId { get; set; } = string.Empty;
    public ReadingSnapshotDto Current { get; set; } = null!;
    public ReadingSnapshotDto? LastMonth { get; set; }
}

public class ReadingsComparisonResponseDto
{
    public string LastMonthHeading { get; set; } = string.Empty;
    public List<StationComparisonRowDto> Rows { get; set; } = new();

    /// <summary><c>limit</c> som skickades mot Trafikverket (efter vår 1–2000-klampning).</summary>
    public int RequestedLimit { get; set; }

    /// <summary>Antal <c>WeatherMeasurepoint</c>-objekt i JSON-svaret för aktuellt läge.</summary>
    public int MeasurepointsInResponse { get; set; }

    /// <summary>Poster utan komplett provtid/lufttemperatur som hoppades över vid parsning.</summary>
    public int SkippedIncompleteMeasurepoints { get; set; }

    /// <summary>Antal avläsningar med komplett data innan senaste-per-station slås ihop.</summary>
    public int ParsedObservationCount { get; set; }
}

/// <summary>Syntetiska historiska punkter från <c>/api/sensor/history</c>.</summary>
public class FakeHistoryPointDto
{
    public string SensorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
