namespace TvTidsserieembedding.Domain.Interfaces;

public interface IAnomalyDetector
{
    Task<bool> IsAnomalyAsync(float[] embedding);
}
