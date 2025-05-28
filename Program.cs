using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellbeingTeamsBot;
using WellbeingTeamsBot.Bots;
using WellbeingTeamsBot.Services;
using WellbeingTeamsBot.Storage;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Use configuration from Azure App Settings
builder.Configuration.AddEnvironmentVariables();

// Bot Authentication (relies on MicrosoftAppId and MicrosoftAppPassword from app settings)
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Adapter with error handling
builder.Services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetRequiredService<CloudAdapter>());

// Bot logic
builder.Services.AddTransient<IBot, TeamsConversationBot>();

// Custom services
builder.Services.AddScoped<ISqlStorageHelper, SqlStorageHelper>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Controllers
builder.Services.AddControllers();

// Manual logging initialization
ManualLogger.Initialize(builder.Configuration);

var app = builder.Build();

// Developer exception page
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

try
{
    ManualLogger.Log("Program.cs starting app...");
    app.Run();
}
catch (Exception ex)
{
    ManualLogger.Log($"FATAL error in Program.cs: {ex.Message}\n{ex.StackTrace}");
    throw;
}
