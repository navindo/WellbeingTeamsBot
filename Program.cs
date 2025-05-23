using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WellbeingTeamsBot;
using WellbeingTeamsBot.Bots;
using WellbeingTeamsBot.Services;
using WellbeingTeamsBot.Storage;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug(); // Optional
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register Bot Framework Authentication
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Register Adapter with error handler
builder.Services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetRequiredService<CloudAdapter>());

// Register Bot
builder.Services.AddTransient<IBot, TeamsConversationBot>();

// Register custom services
builder.Services.AddScoped<ISqlStorageHelper, SqlStorageHelper>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Register MVC Controllers
builder.Services.AddControllers();

var app = builder.Build();

//  Optional: Show stack traces in Dev mode
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization(); // Optional

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Fatal error: " + ex.ToString());
    throw;
}