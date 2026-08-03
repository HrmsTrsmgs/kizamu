using Kizamu.Application.TimeRecords;

var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddSingleton<StartTimeRecord>();

    var app = builder.Build();
    var startedAt = default(DateTimeOffset);
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
    app.MapPost("/api/time-records/start", (StartTimeRecord startTimeRecord) =>
    {
        var record = startTimeRecord.Execute();
        startedAt = record.StartedAt;
        return Results.Json(
            record,
            statusCode: StatusCodes.Status201Created);
    });
    app.MapPost("/api/time-records/{id:guid}/end", (
        Guid id,
        TimeProvider timeProvider) =>
        Results.Ok(new
        {
            Id = id,
            StartedAt = startedAt,
            EndedAt = timeProvider.GetUtcNow(),
        }));

    return app;
}

public partial class Program;
