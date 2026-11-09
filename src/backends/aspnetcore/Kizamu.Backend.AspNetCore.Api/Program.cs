using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;
using Kizamu.Backend.AspNetCore.Api.Endpoints.Health;
using Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddSingleton<TimeRecordStartOperation>();
    builder.Services.AddSingleton<TimeRecordEndOperation>();
    builder.Services.AddSingleton<TimeRecordReader>();
    builder.Services.AddSingleton<ITimeRecordState, InMemoryTimeRecordState>();

    var app = builder.Build();
    app.MapHealthEndpoint();
    app.MapStartTimeRecordEndpoint();
    app.MapEndTimeRecordEndpoint();
    app.MapGetTimeRecordsEndpoint();

    return app;
}

public partial class Program;
