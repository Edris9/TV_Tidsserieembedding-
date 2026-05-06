using TvTidsserieembedding.Domain.Entities;

namespace TvTidsserieembedding.Domain.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(IEnumerable<SensorReading> window);
}
