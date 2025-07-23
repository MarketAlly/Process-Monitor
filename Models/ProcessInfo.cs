using System.ComponentModel.DataAnnotations;

namespace MarketAlly.ProcessMonitor.Models;

/// <summary>
/// Represents process configuration information
/// </summary>
public class ProcessInfo
{
    /// <summary>
    /// Name of the process
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the executable process
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of process instances to maintain
    /// </summary>
    [Range(0, 100)]
    public int Count { get; set; }
    
    /// <summary>
    /// Time of day (HH:mm) to launch the process
    /// </summary>
    public string? Time { get; set; }
    
    /// <summary>
    /// Frequency in minutes to launch the process
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? Interval { get; set; }
    
    /// <summary>
    /// Whether to enable process monitoring for this process
    /// </summary>
    public bool Enable { get; set; }
    
    /// <summary>
    /// Command line arguments for the process
    /// </summary>
    public string? Arguments { get; set; }
    
    /// <summary>
    /// Working directory for the process
    /// </summary>
    public string? WorkingDirectory { get; set; }
    
    /// <summary>
    /// Environment variables for the process
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
    
    /// <summary>
    /// Maximum retries on failure
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Delay between retries in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;
}