using MarketAlly.ProcessMonitor.Models;

namespace MarketAlly.ProcessMonitor.Interfaces;

/// <summary>
/// Handles process scheduling operations
/// </summary>
public interface IScheduler
{
    /// <summary>
    /// Schedules a process to start at a specific time
    /// </summary>
    void ScheduleProcessStart(ProcessInfo processInfo, bool openInNewWindow);
    
    /// <summary>
    /// Schedules a process for repeated execution at specified intervals
    /// </summary>
    void ScheduleRepeatedExecution(ProcessInfo processInfo, bool openInNewWindow);
    
    /// <summary>
    /// Cancels all scheduled tasks for a process
    /// </summary>
    void CancelScheduledTasks(string processName);
    
    /// <summary>
    /// Disposes of all scheduled tasks
    /// </summary>
    void Dispose();
}