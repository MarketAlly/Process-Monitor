namespace MarketAlly.ProcessMonitor.Models;

/// <summary>
/// Root configuration for process monitoring
/// </summary>
public class ProcessConfiguration
{
    /// <summary>
    /// List of processes to monitor
    /// </summary>
    public List<ProcessInfo> Processes { get; set; } = new();
    
    /// <summary>
    /// Configuration version
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}