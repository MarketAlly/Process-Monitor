using MarketAlly.ProcessMonitor.Models;

namespace MarketAlly.ProcessMonitor.Interfaces;

/// <summary>
/// Manages application configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current process configuration
    /// </summary>
    Task<ProcessConfiguration> GetConfigurationAsync();
    
    /// <summary>
    /// Reloads configuration from source
    /// </summary>
    Task ReloadConfigurationAsync();
    
    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    event EventHandler<ProcessConfiguration> ConfigurationChanged;
    
    /// <summary>
    /// Gets application settings
    /// </summary>
    AppSettings GetAppSettings();
}