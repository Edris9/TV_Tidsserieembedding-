using TvTidsserieembedding.Domain.Entities;
using TvTidsserieembedding.Domain.Interfaces;

namespace TvTidsserieembedding.Infrastructure;

public class EmbeddingService : IEmbeddingService
{
    public Task<float[]> EmbedAsync(IEnumerable<SensorReading> window)
    {
        var values = window.Select(r => (float)r.Value).ToArray();
        return Task.FromResult(values);
    }
}
