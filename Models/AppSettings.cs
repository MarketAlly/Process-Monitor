namespace MarketAlly.ProcessMonitor.Models;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Monitoring interval in seconds
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 10;
    
    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;
    
    /// <summary>
    /// Log retention days
    /// </summary>
    public int LogRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Allowed process paths (whitelist)
    /// </summary>
    public List<string> AllowedPaths { get; set; } = new();
    
    /// <summary>
    /// Enable process path validation
    /// </summary>
    public bool EnablePathValidation { get; set; } = true;
    
    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
    
    /// <summary>
    /// Maximum concurrent process starts
    /// </summary>
    public int MaxConcurrentStarts { get; set; } = 5;
}