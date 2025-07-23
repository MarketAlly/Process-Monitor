using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MarketAlly.ProcessMonitor.Models;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Monitors system and process performance metrics
/// </summary>
public class PerformanceMonitoringService : BackgroundService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly AppSettings _appSettings;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private readonly Dictionary<string, ProcessPerformanceInfo> _processMetrics = new();

    public PerformanceMonitoringService(
        ILogger<PerformanceMonitoringService> logger,
        AppSettings appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        if (OperatingSystem.IsWindows() && _appSettings.EnablePerformanceMonitoring)
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_appSettings.EnablePerformanceMonitoring)
        {
            _logger.LogInformation("Performance monitoring is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting performance metrics");
            }
        }
    }

    private async Task CollectMetricsAsync()
    {
        try
        {
            var systemMetrics = GetSystemMetrics();
            _logger.LogInformation("System metrics - CPU: {Cpu:F1}%, Memory Available: {Memory}MB",
                systemMetrics.CpuUsage, systemMetrics.AvailableMemoryMB);

            // Collect process-specific metrics
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    if (_processMetrics.ContainsKey(process.ProcessName))
                    {
                        var metrics = GetProcessMetrics(process);
                        _processMetrics[process.ProcessName] = metrics;
                        
                        if (metrics.WorkingSetMB > 1000) // Log if process uses more than 1GB
                        {
                            _logger.LogWarning("Process {ProcessName} is using {Memory}MB of memory",
                                process.ProcessName, metrics.WorkingSetMB);
                        }
                    }
                }
                catch
                {
                    // Ignore individual process errors
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect metrics");
        }
    }

    private SystemMetrics GetSystemMetrics()
    {
        var metrics = new SystemMetrics();

        if (OperatingSystem.IsWindows() && _cpuCounter != null && _memoryCounter != null)
        {
            try
            {
                metrics.CpuUsage = _cpuCounter.NextValue();
                metrics.AvailableMemoryMB = (long)_memoryCounter.NextValue();
            }
            catch { }
        }
        else
        {
            // Cross-platform fallback
            using var process = Process.GetCurrentProcess();
            metrics.AvailableMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
        }

        return metrics;
    }

    private ProcessPerformanceInfo GetProcessMetrics(Process process)
    {
        return new ProcessPerformanceInfo
        {
            ProcessName = process.ProcessName,
            ProcessId = process.Id,
            WorkingSetMB = process.WorkingSet64 / (1024 * 1024),
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount
        };
    }

    public Dictionary<string, ProcessPerformanceInfo> GetCurrentMetrics()
    {
        return new Dictionary<string, ProcessPerformanceInfo>(_processMetrics);
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        base.Dispose();
    }
}

public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public long AvailableMemoryMB { get; set; }
}

public class ProcessPerformanceInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public long WorkingSetMB { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}