using Kizamu.Application.TimeRecords;
using Kizamu.Infrastructure.TimeRecords;
using Kizamu.Server.Endpoints.TimeRecords;

var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddSingleton<StartTimeRecord>();
    builder.Services.AddSingleton<EndTimeRecord>();
    builder.Services.AddSingleton<ITimeRecordState, InMemoryTimeRecordState>();

    var app = builder.Build();
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
    app.MapPost("/api/time-records/start", (StartTimeRecord startTimeRecord) =>
    {
        var record = startTimeRecord.Execute();
        return Results.Json(
            new StartTimeRecordResponse(record.Id, record.StartedAt),
            statusCode: StatusCodes.Status201Created);
    });
    app.MapPost("/api/time-records/{id:guid}/end", (Guid id, EndTimeRecord endTimeRecord) =>
        Results.Ok(
            new EndTimeRecordResponse(
                endTimeRecord.Execute(id))));

    return app;
}

public partial class Program;
