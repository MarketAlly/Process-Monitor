using System.Diagnostics;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using MarketAlly.ProcessMonitor.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Manages process lifecycle operations with retry logic and error handling
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly IProcessValidator _validator;
    private readonly AsyncRetryPolicy<bool> _retryPolicy;
    private readonly SemaphoreSlim _startSemaphore;

    public ProcessManager(ILogger<ProcessManager> logger, IProcessValidator validator, AppSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _startSemaphore = new SemaphoreSlim(settings.MaxConcurrentStarts);
        
        _retryPolicy = Policy<bool>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(outcome.Exception, 
                        "Retry {RetryCount} after {TimeSpan}s for process {ProcessName}", 
                        retryCount, timeSpan.TotalSeconds, context["ProcessName"]);
                });
    }

    public async Task<bool> StartProcessAsync(ProcessInfo processInfo, bool openInNewWindow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processInfo);
        
        var validationResult = _validator.ValidateProcessInfo(processInfo);
        if (!validationResult.IsValid)
        {
            _logger.LogError("Process validation failed for {ProcessName}: {Errors}", 
                processInfo.Name, string.Join(", ", validationResult.Errors));
            throw new ProcessValidationException($"Validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        await _startSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await _retryPolicy.ExecuteAsync(async (context) =>
            {
                _logger.LogInformation("Starting process {ProcessName} from {ProcessPath}", 
                    processInfo.Name, processInfo.Path);

                var startInfo = new ProcessStartInfo
                {
                    FileName = processInfo.Path,
                    Arguments = processInfo.Arguments ?? string.Empty,
                    WorkingDirectory = processInfo.WorkingDirectory ?? Path.GetDirectoryName(processInfo.Path),
                    CreateNoWindow = !openInNewWindow,
                    UseShellExecute = openInNewWindow,
                    RedirectStandardOutput = !openInNewWindow,
                    RedirectStandardError = !openInNewWindow
                };

                if (processInfo.EnvironmentVariables != null)
                {
                    foreach (var kvp in processInfo.EnvironmentVariables)
                    {
                        startInfo.Environment[kvp.Key] = kvp.Value;
                    }
                }

                try
                {
                    var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        throw new ProcessStartException($"Failed to start process {processInfo.Name}");
                    }

                    _logger.LogInformation("Successfully started process {ProcessName} with PID {ProcessId}", 
                        processInfo.Name, process.Id);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start process {ProcessName}", processInfo.Name);
                    throw new ProcessStartException($"Failed to start process {processInfo.Name}", ex);
                }
            }, new Context { ["ProcessName"] = processInfo.Name });
        }
        finally
        {
            _startSemaphore.Release();
        }
    }

    public async Task EnsureProcessRunningAsync(ProcessInfo processInfo, int desiredCount, bool openInNewWindow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processInfo);
        
        var runningCount = GetRunningProcessCount(processInfo.Name);
        var toStart = Math.Max(0, desiredCount - runningCount);
        
        if (toStart == 0)
        {
            _logger.LogDebug("Process {ProcessName} already has {Count} instances running", 
                processInfo.Name, runningCount);
            return;
        }

        _logger.LogInformation("Starting {Count} instances of {ProcessName}", toStart, processInfo.Name);
        
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < toStart; i++)
        {
            tasks.Add(StartProcessAsync(processInfo, openInNewWindow, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public int GetRunningProcessCount(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process count for {ProcessName}", processName);
            return 0;
        }
    }

    public async Task<bool> StopProcessAsync(string processName, CancellationToken cancellationToken = default)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _logger.LogInformation("No running instances of {ProcessName} found", processName);
                return true;
            }

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    await process.WaitForExitAsync(cancellationToken);
                    _logger.LogInformation("Stopped process {ProcessName} with PID {ProcessId}", 
                        processName, process.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop process {ProcessName} with PID {ProcessId}", 
                        processName, process.Id);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping process {ProcessName}", processName);
            return false;
        }
    }
}