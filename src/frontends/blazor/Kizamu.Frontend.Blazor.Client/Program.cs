using Kizamu.Frontend.Blazor.Client;
using Kizamu.Frontend.Blazor.Client.Health;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<HealthApi>();
builder.Services.AddScoped<TimeRecordApi>();
builder.Services.AddSingleton(TimeProvider.System);
await builder.Build().RunAsync();
