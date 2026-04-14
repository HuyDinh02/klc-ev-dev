using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace KLC;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting KLC.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);

            // Sentry: capture errors via ILogger integration
            // Note: AddSentry() (full ASP.NET Core middleware) conflicts with ABP's
            // initialization pipeline. Using AddLogging().AddSentry() is safe and captures
            // all unhandled exceptions + ILogger.LogError calls with stack traces.
            var sentryDsn = builder.Configuration["Sentry:Dsn"];
            if (!string.IsNullOrEmpty(sentryDsn))
            {
                builder.Services.AddLogging(logging => logging.AddSentry(o =>
                {
                    o.Dsn = sentryDsn;
                    o.Environment = builder.Environment.EnvironmentName;
                    o.TracesSampleRate = builder.Environment.IsProduction() ? 0.1 : 1.0;
                    o.SendDefaultPii = false;
                    o.MinimumEventLevel = LogLevel.Error;
                }));
            }

            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            await builder.AddApplicationAsync<KLCHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
