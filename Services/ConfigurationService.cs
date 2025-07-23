using System.Text.Json;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Manages application configuration with hot reload support
/// </summary>
public class ConfigurationService : IConfigurationService, IDisposable
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly string _configFilePath;
    private readonly SemaphoreSlim _configLock = new(1, 1);

    public event EventHandler<ProcessConfiguration>? ConfigurationChanged;

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IConfiguration configuration,
        IMemoryCache cache,
        IOptionsMonitor<AppSettings> appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        _configFilePath = Path.Combine(AppContext.BaseDirectory, "processlist.json");
        _fileWatcher = InitializeFileWatcher();
    }

    public async Task<ProcessConfiguration> GetConfigurationAsync()
    {
        const string cacheKey = "ProcessConfiguration";
        
        if (_cache.TryGetValue<ProcessConfiguration>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        await _configLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue<ProcessConfiguration>(cacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var config = await LoadConfigurationAsync();
            
            _cache.Set(cacheKey, config, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            });

            return config;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task ReloadConfigurationAsync()
    {
        _logger.LogInformation("Reloading process configuration");
        
        await _configLock.WaitAsync();
        try
        {
            _cache.Remove("ProcessConfiguration");
            var config = await LoadConfigurationAsync();
            
            _cache.Set("ProcessConfiguration", config, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            });

            ConfigurationChanged?.Invoke(this, config);
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            throw;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public AppSettings GetAppSettings()
    {
        return _appSettings.CurrentValue;
    }

    private async Task<ProcessConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogError("Configuration file not found at {Path}", _configFilePath);
                throw new FileNotFoundException($"Configuration file not found: {_configFilePath}");
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<ProcessConfiguration>(json, options);
            
            if (config == null || config.Processes == null)
            {
                throw new InvalidOperationException("Invalid configuration format");
            }

            config.LastModified = File.GetLastWriteTimeUtc(_configFilePath);
            
            _logger.LogInformation("Loaded configuration with {Count} processes", config.Processes.Count);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {Path}", _configFilePath);
            throw;
        }
    }

    private FileSystemWatcher InitializeFileWatcher()
    {
        var directory = Path.GetDirectoryName(_configFilePath) ?? AppContext.BaseDirectory;
        var fileName = Path.GetFileName(_configFilePath);

        var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += async (sender, e) =>
        {
            // Debounce file changes
            await Task.Delay(500);
            
            try
            {
                _logger.LogInformation("Configuration file changed, reloading");
                await ReloadConfigurationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling configuration file change");
            }
        };

        return watcher;
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _configLock?.Dispose();
    }
}