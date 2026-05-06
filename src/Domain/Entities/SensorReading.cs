namespace TvTidsserieembedding.Domain.Entities;

public class SensorReading
{
    public string SensorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}
