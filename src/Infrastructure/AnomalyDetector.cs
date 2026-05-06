using TvTidsserieembedding.Domain.Interfaces;

namespace TvTidsserieembedding.Infrastructure;

public class AnomalyDetector : IAnomalyDetector
{
    private readonly List<float[]> _historicalEmbeddings = new();

    public Task<bool> IsAnomalyAsync(float[] embedding)
    {
        if (_historicalEmbeddings.Count == 0)
        {
            _historicalEmbeddings.Add(embedding);
            return Task.FromResult(false);
        }

        var avgSimilarity = _historicalEmbeddings
            .Select(h => CosineSimilarity(h, embedding))
            .Average();

        _historicalEmbeddings.Add(embedding);

        return Task.FromResult(avgSimilarity < 0.85f);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = a.Zip(b, (x, y) => x * y).Sum();
        var magA = MathF.Sqrt(a.Sum(x => x * x));
        var magB = MathF.Sqrt(b.Sum(x => x * x));
        return dot / (magA * magB);
    }
}
