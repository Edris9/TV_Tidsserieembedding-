var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok("TV Tidsserieembedding API"));

app.Run();
