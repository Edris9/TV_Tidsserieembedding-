using TvTidsserieembedding.Domain.Entities;

namespace TvTidsserieembedding.Domain.Interfaces;

public interface ISensorRepository
{
    Task<IEnumerable<SensorReading>> GetHistoricalAsync(string sensorId, DateTime from, DateTime to);
    Task SaveAsync(SensorReading reading);
}
