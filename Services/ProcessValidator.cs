using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using Microsoft.Extensions.Logging;

namespace MarketAlly.ProcessMonitor.Services;

/// <summary>
/// Validates process configurations for security and correctness
/// </summary>
public class ProcessValidator : IProcessValidator
{
    private readonly ILogger<ProcessValidator> _logger;
    private readonly AppSettings _appSettings;
    private readonly HashSet<string> _allowedPaths;
    private static readonly Regex TimeFormatRegex = new(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.Compiled);

    public ProcessValidator(ILogger<ProcessValidator> logger, AppSettings appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _allowedPaths = new HashSet<string>(_appSettings.AllowedPaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public bool ValidateProcessPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Process path is null or empty");
            return false;
        }

        try
        {
            // Ensure absolute path
            if (!Path.IsPathRooted(path))
            {
                _logger.LogWarning("Process path is not absolute: {Path}", path);
                return false;
            }

            // Normalize path
            var normalizedPath = Path.GetFullPath(path);

            // Check if file exists
            if (!File.Exists(normalizedPath))
            {
                _logger.LogWarning("Process file does not exist: {Path}", normalizedPath);
                return false;
            }

            // Check against whitelist if enabled
            if (_appSettings.EnablePathValidation && _allowedPaths.Count > 0)
            {
                var isAllowed = _allowedPaths.Any(allowedPath => 
                    normalizedPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    _logger.LogWarning("Process path not in allowed list: {Path}", normalizedPath);
                    return false;
                }
            }

            // Verify it's an executable
            var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
            var executableExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1", ".sh" };
            
            if (!executableExtensions.Contains(extension))
            {
                _logger.LogWarning("File is not an executable: {Path}", normalizedPath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating process path: {Path}", path);
            return false;
        }
    }

    public ValidationResult ValidateProcessInfo(ProcessInfo processInfo)
    {
        var errors = new List<string>();

        if (processInfo == null)
        {
            errors.Add("Process info is null");
            return new ValidationResult(false, errors);
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(processInfo.Name))
        {
            errors.Add("Process name is required");
        }
        else if (processInfo.Name.Length > 260)
        {
            errors.Add("Process name is too long");
        }

        // Validate path
        if (!ValidateProcessPath(processInfo.Path))
        {
            errors.Add($"Invalid process path: {processInfo.Path}");
        }

        // Validate count
        if (processInfo.Count < 0 || processInfo.Count > 100)
        {
            errors.Add($"Process count must be between 0 and 100, got {processInfo.Count}");
        }

        // Validate time format
        if (!string.IsNullOrWhiteSpace(processInfo.Time) && !TimeFormatRegex.IsMatch(processInfo.Time))
        {
            errors.Add($"Invalid time format: {processInfo.Time}. Expected HH:mm");
        }

        // Validate interval
        if (processInfo.Interval.HasValue && processInfo.Interval.Value <= 0)
        {
            errors.Add($"Interval must be positive, got {processInfo.Interval.Value}");
        }

        // Validate working directory
        if (!string.IsNullOrWhiteSpace(processInfo.WorkingDirectory))
        {
            if (!Directory.Exists(processInfo.WorkingDirectory))
            {
                errors.Add($"Working directory does not exist: {processInfo.WorkingDirectory}");
            }
        }

        // Validate arguments for potential injection
        if (!string.IsNullOrWhiteSpace(processInfo.Arguments))
        {
            var dangerousPatterns = new[] { ";", "|", "&", "`", "$(" };
            if (dangerousPatterns.Any(pattern => processInfo.Arguments.Contains(pattern)))
            {
                errors.Add("Arguments contain potentially dangerous characters");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    public async Task<bool> CheckPermissionsAsync(string path)
    {
        try
        {
            // Check if we can read the file
            using (var stream = File.OpenRead(path))
            {
                // File is readable
            }

            // Check if running as administrator (Windows)
            if (OperatingSystem.IsWindows())
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                
                if (!isAdmin)
                {
                    _logger.LogWarning("Not running as administrator, some processes may fail to start");
                }
            }

            return await Task.FromResult(true);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("No permission to access file: {Path}", path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permissions for: {Path}", path);
            return false;
        }
    }
}