using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using MarketAlly.ProcessMonitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace MarketAlly.ProcessMonitor;

public class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/processmonitor-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Process Monitor");
            await CreateHostBuilder(args).RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure app settings
                services.Configure<AppSettings>(context.Configuration.GetSection("ProcessMonitor"));
                
                // Register services
                services.AddSingleton<IMemoryCache, MemoryCache>();
                services.AddSingleton(provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var appSettings = new AppSettings();
                    configuration.GetSection("ProcessMonitor").Bind(appSettings);
                    return appSettings;
                });
                
                services.AddSingleton<IProcessValidator, ProcessValidator>();
                services.AddSingleton<IProcessManager, ProcessManager>();
                services.AddSingleton<IScheduler, ProcessScheduler>();
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                
                // Register hosted services
                services.AddHostedService<ProcessMonitorService>();
                services.AddHostedService<PerformanceMonitoringService>();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });
            })
            .UseConsoleLifetime();
}