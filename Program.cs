using AiStructuredDataFromImageExtractionDemo.Components;
using AiStructuredDataFromImageExtractionDemo.Services;
using Havit.Blazor.Components.Web;

// Default to Development so MapStaticAssets serves _framework/* from NuGet without a publish step.
// Override by exporting ASPNETCORE_ENVIRONMENT before `dotnet run`.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
	Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents(o => o.DetailedErrors = true)
	.AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);

builder.Services.AddHxServices();
builder.Services.AddHxMessenger();

var endpoint = builder.Configuration["AzureOpenAi:Endpoint"]
	?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
	?? "https://crha-mm394332-switzerlandnorth.services.ai.azure.com/";
var apiKey = builder.Configuration["AzureOpenAi:ApiKey"]
	?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
	?? "MISSING";
if (apiKey == "MISSING")
	Console.Error.WriteLine("⚠ AZURE_OPENAI_API_KEY not set — UI will load but extraction will fail. Set via 'dotnet user-secrets set AzureOpenAi:ApiKey <key>' or the AZURE_OPENAI_API_KEY env var.");

builder.Services.AddSingleton(sp => new ExtractionService(endpoint, apiKey));
builder.Services.AddSingleton<ExtractionRunner>();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
