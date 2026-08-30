using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScheduleMaker.App;
using ScheduleMaker.App.Application;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ILocalStorage, BrowserLocalStorage>();
builder.Services.AddScoped<IApplicationStatePersistence, LocalStorageApplicationStatePersistence>();
builder.Services.AddScoped<ApplicationStateStore>();
builder.Services.AddScoped<ConfigurationStateService>();

var host = builder.Build();
await host.Services.GetRequiredService<ApplicationStateStore>().InitializeAsync();
await host.RunAsync();
