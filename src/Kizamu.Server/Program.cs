using Kizamu.Application.TimeRecords;
using Kizamu.Infrastructure.TimeRecords;
using Kizamu.Server.Endpoints.Health;
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
    app.MapHealthEndpoint();
    app.MapStartTimeRecordEndpoint();
    app.MapEndTimeRecordEndpoint();

    return app;
}

public partial class Program;
