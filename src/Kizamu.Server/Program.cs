var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/time-records/start", () =>
    Results.Json(
        new { Id = Guid.NewGuid(), StartedAt = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero) },
        statusCode: StatusCodes.Status201Created));

app.Run();

public partial class Program;
