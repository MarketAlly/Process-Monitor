using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MarketAlly.ProcessMonitor.Services;
using MarketAlly.ProcessMonitor.Interfaces;

namespace MarketAlly.ProcessMonitor.Extensions;

/// <summary>
/// Health check extensions for monitoring application health
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddProcessMonitorHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ConfigurationHealthCheck>("configuration", tags: new[] { "ready" })
            .AddCheck<ProcessManagerHealthCheck>("process_manager", tags: new[] { "live" })
            .AddCheck<DiskSpaceHealthCheck>("disk_space", tags: new[] { "ready" });

        return services;
    }
}

/// <summary>
/// Checks if configuration is accessible and valid
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;

    public ConfigurationHealthCheck(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configurationService.GetConfigurationAsync();
            
            if (config?.Processes == null || config.Processes.Count == 0)
            {
                return HealthCheckResult.Unhealthy("No processes configured");
            }

            return HealthCheckResult.Healthy($"Configuration loaded with {config.Processes.Count} processes");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Configuration check failed", ex);
        }
    }
}

/// <summary>
/// Checks if process manager is operational
/// </summary>
public class ProcessManagerHealthCheck : IHealthCheck
{
    private readonly IProcessManager _processManager;

    public ProcessManagerHealthCheck(IProcessManager processManager)
    {
        _processManager = processManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple check to ensure process manager can query running processes
            var count = _processManager.GetRunningProcessCount("System");
            return await Task.FromResult(HealthCheckResult.Healthy("Process manager is operational"));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Process manager check failed", ex);
        }
    }
}

/// <summary>
/// Checks available disk space for logs
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private const long MinimumFreeMegabytes = 100;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
            var drive = new DriveInfo(Path.GetPathRoot(logPath) ?? "C:\\");
            
            var freeSpaceMb = drive.AvailableFreeSpace / (1024 * 1024);
            
            if (freeSpaceMb < MinimumFreeMegabytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Low disk space: {freeSpaceMb}MB available"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space OK: {freeSpaceMb}MB available"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}