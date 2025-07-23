using MarketAlly.ProcessMonitor.Models;

namespace MarketAlly.ProcessMonitor.Interfaces;

/// <summary>
/// Manages process lifecycle operations
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Starts a process with the specified configuration
    /// </summary>
    Task<bool> StartProcessAsync(ProcessInfo processInfo, bool openInNewWindow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures the specified number of process instances are running
    /// </summary>
    Task EnsureProcessRunningAsync(ProcessInfo processInfo, int desiredCount, bool openInNewWindow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of running processes by name
    /// </summary>
    int GetRunningProcessCount(string processName);
    
    /// <summary>
    /// Stops all instances of a process
    /// </summary>
    Task<bool> StopProcessAsync(string processName, CancellationToken cancellationToken = default);
}