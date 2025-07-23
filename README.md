# Process Monitor Utility

A production-ready process monitoring and management tool built with .NET 9, featuring enterprise-grade security, comprehensive error handling, and robust scheduling capabilities.

## Overview

Process Monitor is a sophisticated utility that automates process lifecycle management with features including:
- Scheduled process execution
- Continuous process monitoring
- Automatic process restart with retry logic
- Configuration hot-reload
- Comprehensive logging with Serilog
- Security validation and path whitelisting
- Performance monitoring
- Docker support
- Health checks

## Features

### Core Functionality
- **Flexible Process Management**: Start processes at specific times, run continuously, or schedule at intervals
- **Retry Logic**: Automatic retry with exponential backoff on process failures
- **Concurrent Execution Control**: Limit simultaneous process starts
- **Process Count Management**: Ensure specific number of instances are always running

### Security
- **Path Validation**: Whitelist-based executable path validation
- **Argument Sanitization**: Protection against command injection
- **Digital Signature Verification**: Optional executable signature checking
- **Non-root Execution**: Docker container runs as non-privileged user

### Monitoring & Observability
- **Structured Logging**: Serilog with file and console sinks
- **Performance Monitoring**: CPU and memory usage tracking
- **Health Checks**: Configuration, process manager, and disk space checks
- **Log Rotation**: Automatic daily log rotation with retention

### Configuration
- **Hot Reload**: Automatic configuration reload on file changes
- **Environment Variables**: Override settings via environment variables
- **JSON Schema Validation**: Strongly-typed configuration with validation

## Requirements

- .NET 9.0 Runtime or SDK
- Windows, Linux, or macOS
- Docker (optional, for containerized deployment)

## Installation

### From Source
```bash
git clone https://github.com/MarketAlly/Process-Monitor.git
cd Process-Monitor
dotnet restore
dotnet build -c Release
```

### Docker
```bash
docker build -t marketally/processmonitor:latest .
```

## Configuration

### appsettings.json
```json
{
  "ProcessMonitor": {
    "MonitoringIntervalSeconds": 10,
    "EnableDebugLogging": false,
    "LogRetentionDays": 30,
    "AllowedPaths": [
      "C:\\Windows\\System32",
      "C:\\Program Files"
    ],
    "EnablePathValidation": true,
    "EnablePerformanceMonitoring": true,
    "MaxConcurrentStarts": 5
  }
}
```

### processlist.json
```json
{
  "version": "1.0",
  "processes": [
    {
      "name": "MyApp",
      "path": "C:\\Program Files\\MyApp\\app.exe",
      "count": 2,
      "enable": true,
      "interval": 30,
      "time": "08:00",
      "arguments": "--config production",
      "workingDirectory": "C:\\Program Files\\MyApp",
      "environmentVariables": {
        "NODE_ENV": "production"
      },
      "maxRetries": 3,
      "retryDelaySeconds": 5
    }
  ]
}
```

## Usage

### Command Line
```bash
# Run with default configuration
dotnet MarketAlly.ProcessMonitor.dll

# Run with custom environment
dotnet MarketAlly.ProcessMonitor.dll --environment Production

# Override configuration via environment variables
ProcessMonitor__MonitoringIntervalSeconds=30 dotnet MarketAlly.ProcessMonitor.dll
```

### Docker
```bash
# Using docker-compose
docker-compose up -d

# Using docker run
docker run -d \
  --name process-monitor \
  -v $(pwd)/processlist.json:/app/processlist.json:ro \
  -v $(pwd)/logs:/app/logs \
  marketally/processmonitor:latest
```

## Process Configuration Options

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| name | string | Yes | Process identifier |
| path | string | Yes | Absolute path to executable |
| count | int | No | Number of instances to maintain (0-100) |
| enable | bool | Yes | Enable/disable monitoring |
| time | string | No | Start time in HH:mm format |
| interval | int | No | Repeat interval in minutes |
| arguments | string | No | Command line arguments |
| workingDirectory | string | No | Working directory path |
| environmentVariables | object | No | Environment variables |
| maxRetries | int | No | Maximum retry attempts (default: 3) |
| retryDelaySeconds | int | No | Delay between retries (default: 5) |

## Architecture

The application follows SOLID principles with a clean architecture:

```
├── Interfaces/          # Service contracts
├── Services/           # Service implementations
├── Models/             # Data models
├── Configuration/      # Configuration handling
├── Validators/         # Security and validation
├── Extensions/         # Extension methods
├── Exceptions/         # Custom exceptions
└── Tests/              # Unit and integration tests
```

### Key Components
- **ProcessManager**: Handles process lifecycle with retry logic
- **ProcessScheduler**: Manages scheduled tasks
- **ConfigurationService**: Hot-reload configuration management
- **ProcessValidator**: Security validation
- **ProcessMonitorService**: Main background service
- **PerformanceMonitoringService**: System metrics collection

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test category
dotnet test --filter Category=Unit
```

## Deployment

### Production Checklist
1. ✅ Update `appsettings.Production.json` with production values
2. ✅ Configure allowed paths for your environment
3. ✅ Set appropriate log retention period
4. ✅ Enable performance monitoring if needed
5. ✅ Configure health check endpoints
6. ✅ Set up log aggregation (e.g., to Seq, ELK)
7. ✅ Configure monitoring and alerting
8. ✅ Review security settings

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: process-monitor
spec:
  replicas: 1
  selector:
    matchLabels:
      app: process-monitor
  template:
    metadata:
      labels:
        app: process-monitor
    spec:
      containers:
      - name: process-monitor
        image: marketally/processmonitor:latest
        volumeMounts:
        - name: config
          mountPath: /app/processlist.json
          subPath: processlist.json
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: config
        configMap:
          name: process-config
      - name: logs
        persistentVolumeClaim:
          claimName: process-monitor-logs
```

## Monitoring

### Health Checks
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Metrics
- Process count by name
- CPU and memory usage
- Scheduled task execution
- Error rates and retry counts

### Logging
Logs are written to:
- Console (structured JSON in production)
- File: `logs/processmonitor-YYYYMMDD.log`

## Security Considerations

1. **Path Whitelisting**: Only executables in allowed paths can be started
2. **Argument Validation**: Protection against shell injection
3. **File Permissions**: Ensure proper permissions on configuration files
4. **Secrets Management**: Use environment variables or secret stores for sensitive data
5. **Network Isolation**: Run in isolated network when possible

## Troubleshooting

### Common Issues

**Process fails to start**
- Check path validation and whitelist
- Verify file permissions
- Check logs for detailed error messages

**Configuration not reloading**
- Ensure file watcher has permissions
- Check for file lock issues
- Verify JSON syntax

**High memory usage**
- Enable performance monitoring
- Check for process leaks
- Review retry settings

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE.txt file for details.

## Support

For issues and feature requests, please use the GitHub issue tracker.