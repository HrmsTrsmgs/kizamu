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
    var sameOriginEndpoints = app.MapGroup("").AddEndpointFilter(async (context, next) =>
    {
        if (context.HttpContext.Request.Headers["Sec-Fetch-Site"] != "same-origin")
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        return await next(context);
    });
    sameOriginEndpoints.MapStartTimeRecordEndpoint();
    sameOriginEndpoints.MapEndTimeRecordEndpoint();
    app.MapGetTimeRecordsEndpoint();

    return app;
}

public partial class Program;
