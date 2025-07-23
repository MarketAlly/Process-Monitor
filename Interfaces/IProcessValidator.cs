using MarketAlly.ProcessMonitor.Models;

namespace MarketAlly.ProcessMonitor.Interfaces;

/// <summary>
/// Validates process configurations and paths
/// </summary>
public interface IProcessValidator
{
    /// <summary>
    /// Validates a process path for security
    /// </summary>
    bool ValidateProcessPath(string path);
    
    /// <summary>
    /// Validates entire process configuration
    /// </summary>
    ValidationResult ValidateProcessInfo(ProcessInfo processInfo);
    
    /// <summary>
    /// Checks if process has required permissions
    /// </summary>
    Task<bool> CheckPermissionsAsync(string path);
}

public record ValidationResult(bool IsValid, List<string> Errors);