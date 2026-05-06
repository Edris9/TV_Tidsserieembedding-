using TvTidsserieembedding.Domain.Interfaces;

namespace TvTidsserieembedding.Application.Services;

public class SensorAnalysisService
{
    private readonly ISensorRepository _repo;
    private readonly IEmbeddingService _embedding;
    private readonly IAnomalyDetector _detector;

    public SensorAnalysisService(ISensorRepository repo, IEmbeddingService embedding, IAnomalyDetector detector)
    {
        _repo = repo;
        _embedding = embedding;
        _detector = detector;
    }

    public async Task<bool> AnalyzeAsync(string sensorId, DateTime from, DateTime to)
    {
        var readings = await _repo.GetHistoricalAsync(sensorId, from, to);
        var embedding = await _embedding.EmbedAsync(readings);
        return await _detector.IsAnomalyAsync(embedding);
    }
}
