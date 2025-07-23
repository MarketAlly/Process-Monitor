namespace MarketAlly.ProcessMonitor.Exceptions;

/// <summary>
/// Base exception for process monitor
/// </summary>
public class ProcessMonitorException : Exception
{
    public ProcessMonitorException() { }
    public ProcessMonitorException(string message) : base(message) { }
    public ProcessMonitorException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when process validation fails
/// </summary>
public class ProcessValidationException : ProcessMonitorException
{
    public ProcessValidationException() { }
    public ProcessValidationException(string message) : base(message) { }
    public ProcessValidationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when process fails to start
/// </summary>
public class ProcessStartException : ProcessMonitorException
{
    public ProcessStartException() { }
    public ProcessStartException(string message) : base(message) { }
    public ProcessStartException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when configuration is invalid
/// </summary>
public class ConfigurationException : ProcessMonitorException
{
    public ConfigurationException() { }
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}