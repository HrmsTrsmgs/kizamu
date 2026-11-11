using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;
using Kizamu.Backend.AspNetCore.Api.Endpoints.Health;
using Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

var app = CreateApplication(args);
app.Run();

static WebApplication CreateApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    AddTimeRecordServices(builder.Services);

    var app = builder.Build();
    app.MapHealthEndpoint();

    var sameOriginEndpoints = app.MapGroup("").AddEndpointFilter(RequireSameOrigin);
    sameOriginEndpoints.MapStartTimeRecordEndpoint();
    sameOriginEndpoints.MapEndTimeRecordEndpoint();

    app.MapGetTimeRecordsEndpoint();

    return app;
}

static void AddTimeRecordServices(IServiceCollection services)
{
    services.AddSingleton<TimeProvider>(TimeProvider.System);
    services.AddSingleton<TimeRecordStartOperation>();
    services.AddSingleton<TimeRecordEndOperation>();
    services.AddSingleton<TimeRecordReader>();
    services.AddSingleton<ITimeRecordState, InMemoryTimeRecordState>();
}

static async ValueTask<object?> RequireSameOrigin(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{
    if (context.HttpContext.Request.Headers["Sec-Fetch-Site"] != "same-origin")
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    return await next(context);
}

public partial class Program;
