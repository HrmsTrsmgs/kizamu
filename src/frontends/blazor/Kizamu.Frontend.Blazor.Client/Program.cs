using Kizamu.Frontend.Blazor.Client;
using Kizamu.Frontend.Blazor.Infrastructure.Health;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IHealthService, HealthApi>();
builder.Services.AddTransient<HealthStatusViewModel>();
builder.Services.AddScoped<ITimeRecordService, TimeRecordApi>();
builder.Services.AddTransient<ITimeRecorderViewModel, TimeRecorderViewModel>();
builder.Services.AddTransient<ProjectTabsViewModel>();
builder.Services.AddSingleton(TimeProvider.System);
await builder.Build().RunAsync();
