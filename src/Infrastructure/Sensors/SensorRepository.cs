using Microsoft.EntityFrameworkCore;
using TvTidsserieembedding.Domain.Entities;
using TvTidsserieembedding.Domain.Interfaces;
using TvTidsserieembedding.Infrastructure.Persistence;

namespace TvTidsserieembedding.Infrastructure.Sensors;

public class SensorRepository : ISensorRepository
{
    private readonly AppDbContext _db;

    public SensorRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SensorReading>> GetHistoricalAsync(string sensorId, DateTime from, DateTime to)
    {
        return await _db.SensorReadings
            .Where(r => r.SensorId == sensorId && r.Timestamp >= from && r.Timestamp <= to)
            .ToListAsync();
    }

    public async Task SaveAsync(SensorReading reading)
    {
        _db.SensorReadings.Add(reading);
        await _db.SaveChangesAsync();
    }
}
