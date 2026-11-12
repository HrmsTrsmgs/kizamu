using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;
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
    builder.Services.AddSingleton<HttpClient>(_ => new());
    builder.Services.AddSingleton(services =>
        new GoogleIdTokenVerifier(builder.Configuration["GOOGLE_CLIENT_ID"] ?? "", services.GetRequiredService<HttpClient>()));

    var app = builder.Build();
    app.MapHealthEndpoint();

    var owners = new System.Collections.Concurrent.ConcurrentDictionary<Guid, string>();
    var sameOriginEndpoints = app.MapGroup("").AddEndpointFilter(RequireSameOrigin);
    sameOriginEndpoints.MapStartTimeRecordEndpoint(owners);
    sameOriginEndpoints.MapEndTimeRecordEndpoint();

    app.MapGetTimeRecordsEndpoint(owners);

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
