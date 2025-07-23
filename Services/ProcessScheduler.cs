using System.Collections.Concurrent;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using Microsoft.Extensions.Logging;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Handles process scheduling with proper cleanup and error handling
/// </summary>
public class ProcessScheduler : IScheduler, IDisposable
{
    private readonly ILogger<ProcessScheduler> _logger;
    private readonly IProcessManager _processManager;
    private readonly ConcurrentDictionary<string, List<Timer>> _scheduledTasks;
    private bool _disposed;

    public ProcessScheduler(ILogger<ProcessScheduler> logger, IProcessManager processManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _scheduledTasks = new ConcurrentDictionary<string, List<Timer>>();
    }

    public void ScheduleProcessStart(ProcessInfo processInfo, bool openInNewWindow)
    {
        ArgumentNullException.ThrowIfNull(processInfo);
        
        if (string.IsNullOrWhiteSpace(processInfo.Time))
        {
            _logger.LogWarning("Cannot schedule process {ProcessName} without a time", processInfo.Name);
            return;
        }

        try
        {
            var scheduledTime = DateTime.Today.Add(TimeSpan.Parse(processInfo.Time));
            var delay = scheduledTime - DateTime.Now;

            if (delay < TimeSpan.Zero)
            {
                delay = delay.Add(TimeSpan.FromDays(1));
                scheduledTime = scheduledTime.AddDays(1);
            }

            var timer = new Timer(async _ =>
            {
                try
                {
                    var runningCount = _processManager.GetRunningProcessCount(processInfo.Name);
                    if (runningCount == 0)
                    {
                        await _processManager.StartProcessAsync(processInfo, openInNewWindow);
                        _logger.LogInformation("Scheduled process {ProcessName} started at {Time}", 
                            processInfo.Name, DateTime.Now);
                    }
                    else
                    {
                        _logger.LogInformation("Process {ProcessName} already running, skipping scheduled start", 
                            processInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled start for process {ProcessName}", processInfo.Name);
                }
            }, null, delay, Timeout.InfiniteTimeSpan);

            AddScheduledTask(processInfo.Name, timer);
            
            _logger.LogInformation("Scheduled {ProcessName} to start at {ScheduledTime}", 
                processInfo.Name, scheduledTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule process {ProcessName}", processInfo.Name);
            throw;
        }
    }

    public void ScheduleRepeatedExecution(ProcessInfo processInfo, bool openInNewWindow)
    {
        ArgumentNullException.ThrowIfNull(processInfo);
        
        if (!processInfo.Interval.HasValue || processInfo.Interval.Value <= 0)
        {
            _logger.LogWarning("Cannot schedule repeated execution for {ProcessName} without valid interval", 
                processInfo.Name);
            return;
        }

        try
        {
            var timer = new Timer(async _ =>
            {
                try
                {
                    var runningCount = _processManager.GetRunningProcessCount(processInfo.Name);
                    if (runningCount == 0)
                    {
                        await _processManager.StartProcessAsync(processInfo, openInNewWindow);
                        _logger.LogInformation("Repeated execution: started {ProcessName} at {Time}", 
                            processInfo.Name, DateTime.Now);
                    }
                    else
                    {
                        _logger.LogDebug("Process {ProcessName} already running, skipping interval start", 
                            processInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in repeated execution for process {ProcessName}", processInfo.Name);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(processInfo.Interval.Value));

            AddScheduledTask(processInfo.Name, timer);
            
            _logger.LogInformation("Scheduled {ProcessName} to run every {Interval} minutes", 
                processInfo.Name, processInfo.Interval.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule repeated execution for process {ProcessName}", processInfo.Name);
            throw;
        }
    }

    public void CancelScheduledTasks(string processName)
    {
        if (_scheduledTasks.TryRemove(processName, out var timers))
        {
            foreach (var timer in timers)
            {
                timer?.Dispose();
            }
            _logger.LogInformation("Cancelled all scheduled tasks for {ProcessName}", processName);
        }
    }

    private void AddScheduledTask(string processName, Timer timer)
    {
        _scheduledTasks.AddOrUpdate(processName,
            new List<Timer> { timer },
            (key, list) =>
            {
                list.Add(timer);
                return list;
            });
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var kvp in _scheduledTasks)
        {
            foreach (var timer in kvp.Value)
            {
                timer?.Dispose();
            }
        }
        _scheduledTasks.Clear();
        
        _disposed = true;
        _logger.LogInformation("ProcessScheduler disposed");
    }
}