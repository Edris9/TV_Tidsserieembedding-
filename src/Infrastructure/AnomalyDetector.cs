using TvTidsserieembedding.Domain.Interfaces;

namespace TvTidsserieembedding.Infrastructure;

public class AnomalyDetector : IAnomalyDetector
{
    private static readonly List<float[]> _historicalEmbeddings = new();
    private readonly Random _rng = new();

    public Task<bool> IsAnomalyAsync(float[] embedding)
    {
        if (_historicalEmbeddings.Count < 5)
        {
            var fake = embedding.Select(v => v + (float)(_rng.NextDouble() * 6 - 3)).ToArray();
            _historicalEmbeddings.Add(fake);
            return Task.FromResult(false);
        }

        var avgSimilarity = _historicalEmbeddings
            .Select(h => CosineSimilarity(h, embedding))
            .Average();

        _historicalEmbeddings.Add(embedding);

        return Task.FromResult(avgSimilarity < 0.999f);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var len = Math.Min(a.Length, b.Length);
        var dot = a.Take(len).Zip(b.Take(len), (x, y) => x * y).Sum();
        var magA = MathF.Sqrt(a.Sum(x => x * x));
        var magB = MathF.Sqrt(b.Sum(x => x * x));
        if (magA == 0 || magB == 0) return 1f;
        return dot / (magA * magB);
    }
}