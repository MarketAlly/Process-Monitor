using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Background service that monitors and manages processes
/// </summary>
public class ProcessMonitorService : BackgroundService
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly IProcessManager _processManager;
    private readonly IScheduler _scheduler;
    private readonly IConfigurationService _configurationService;
    private readonly AppSettings _appSettings;
    private readonly Dictionary<string, bool> _initializedProcesses = new();
    private ProcessConfiguration? _currentConfiguration;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IProcessManager processManager,
        IScheduler scheduler,
        IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _appSettings = _configurationService.GetAppSettings();
        
        // Subscribe to configuration changes
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Process Monitor Service starting");

        // Ask user about window preference once at startup
        var openInNewWindow = await GetUserPreferenceAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var configuration = await _configurationService.GetConfigurationAsync();
                
                if (_currentConfiguration == null || 
                    configuration.LastModified != _currentConfiguration.LastModified)
                {
                    _currentConfiguration = configuration;
                    await ProcessConfigurationAsync(configuration, openInNewWindow, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(_appSettings.MonitoringIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in process monitoring loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("Process Monitor Service stopping");
    }

    private async Task<bool> GetUserPreferenceAsync()
    {
        // In production, this could be read from configuration
        // For now, keeping the interactive prompt
        await Task.Run(() =>
        {
            Console.WriteLine("Do you want to start processes in a new window? (yes/no)");
            Console.WriteLine("This preference will be used for all processes during this session.");
        });

        string? userInput = await Task.Run(() => Console.ReadLine());
        return userInput?.ToLower() == "yes";
    }

    private async Task ProcessConfigurationAsync(
        ProcessConfiguration configuration, 
        bool openInNewWindow, 
        CancellationToken cancellationToken)
    {
        foreach (var processInfo in configuration.Processes.Where(p => p.Enable))
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var isFirstRun = !_initializedProcesses.ContainsKey(processInfo.Name);
                
                if (isFirstRun)
                {
                    _initializedProcesses[processInfo.Name] = true;

                    // Schedule if it has interval
                    if (processInfo.Interval.HasValue)
                    {
                        _scheduler.ScheduleRepeatedExecution(processInfo, openInNewWindow);
                    }
                    // Schedule if it has specific time
                    else if (!string.IsNullOrEmpty(processInfo.Time))
                    {
                        _scheduler.ScheduleProcessStart(processInfo, openInNewWindow);
                    }
                }

                // Always ensure continuous processes are running
                if (string.IsNullOrEmpty(processInfo.Time) && !processInfo.Interval.HasValue)
                {
                    await _processManager.EnsureProcessRunningAsync(
                        processInfo, 
                        processInfo.Count, 
                        openInNewWindow, 
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing configuration for {ProcessName}", processInfo.Name);
            }
        }
    }

    private void OnConfigurationChanged(object? sender, ProcessConfiguration newConfiguration)
    {
        _logger.LogInformation("Configuration changed, updating process monitoring");
        
        // Cancel tasks for processes that are no longer enabled
        var disabledProcesses = _initializedProcesses.Keys
            .Where(name => !newConfiguration.Processes.Any(p => p.Name == name && p.Enable))
            .ToList();

        foreach (var processName in disabledProcesses)
        {
            _scheduler.CancelScheduledTasks(processName);
            _initializedProcesses.Remove(processName);
            _logger.LogInformation("Cancelled monitoring for disabled process: {ProcessName}", processName);
        }
    }

    public override void Dispose()
    {
        _configurationService.ConfigurationChanged -= OnConfigurationChanged;
        base.Dispose();
    }
}