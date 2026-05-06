using Microsoft.EntityFrameworkCore;
using TvTidsserieembedding.Application.Services;
using TvTidsserieembedding.Domain.Interfaces;
using TvTidsserieembedding.Infrastructure;
using TvTidsserieembedding.Infrastructure.Persistence;
using TvTidsserieembedding.Infrastructure.Sensors;
using TvTidsserieembedding.Infrastructure.TrafikverketApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IAnomalyDetector, AnomalyDetector>();
builder.Services.AddScoped<SensorAnalysisService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<TrafikverketClient>();

var app = builder.Build();

app.MapGet("/", () => "TV Tidsserieembedding API");
app.MapControllers();

app.Run();