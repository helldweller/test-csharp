using DemoApp.Frontend;
using DemoApp.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.RootComponents.Add<HeadOutlet>("head::after");

// Base address of the backend API (adjust port as needed)
var backendBaseAddress = new Uri("https://localhost:5003/");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = backendBaseAddress
});

builder.Services.AddScoped(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<ILogger<ServerCommunicationService>>();
    return new ServerCommunicationService(httpClient, backendBaseAddress, logger);
});

await builder.Build().RunAsync();
