using Microsoft.EntityFrameworkCore;
using TvTidsserieembedding.Domain.Entities;

namespace TvTidsserieembedding.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
}
