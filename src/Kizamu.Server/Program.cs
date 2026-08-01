var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

    var app = builder.Build();
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
    app.MapPost("/api/time-records/start", (TimeProvider timeProvider) =>
        Results.Json(
            new { Id = Guid.NewGuid(), StartedAt = timeProvider.GetUtcNow() },
            statusCode: StatusCodes.Status201Created));

    return app;
}

public partial class Program;
