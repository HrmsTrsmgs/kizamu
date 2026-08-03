using Kizamu.Application.TimeRecords;

var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddSingleton<StartTimeRecord>();
    builder.Services.AddSingleton<EndTimeRecord>();

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
        EndTimeRecord endTimeRecord) =>
        Results.Ok(endTimeRecord.Execute(id, startedAt)));

    return app;
}

public partial class Program;
