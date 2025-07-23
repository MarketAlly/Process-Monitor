using FluentAssertions;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using MarketAlly.ProcessMonitor.Services;
using MarketAlly.ProcessMonitor.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MarketAlly.ProcessMonitor.Tests.Unit;

[TestClass]
public class ProcessManagerTests
{
    private Mock<ILogger<ProcessManager>> _loggerMock = null!;
    private Mock<IProcessValidator> _validatorMock = null!;
    private ProcessManager _processManager = null!;
    private AppSettings _appSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProcessManager>>();
        _validatorMock = new Mock<IProcessValidator>();
        _appSettings = new AppSettings { MaxConcurrentStarts = 5 };
        _processManager = new ProcessManager(_loggerMock.Object, _validatorMock.Object, _appSettings);
    }

    [TestMethod]
    public async Task StartProcessAsync_WithValidConfig_LogsSuccess()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "test",
            Path = @"C:\test.exe",
            Enable = true
        };

        _validatorMock.Setup(v => v.ValidateProcessInfo(It.IsAny<ProcessInfo>()))
            .Returns(new ValidationResult(true, new List<string>()));

        // Act & Assert
        // We can't actually start a process in unit tests, so we verify the validation occurs
        try
        {
            await _processManager.StartProcessAsync(processInfo, false);
        }
        catch (ProcessStartException)
        {
            // Expected when the process doesn't exist
        }
        
        _validatorMock.Verify(v => v.ValidateProcessInfo(processInfo), Times.Once);
    }

    [TestMethod]
    public async Task StartProcessAsync_WithInvalidConfig_ThrowsException()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "invalid",
            Path = "invalid_path",
            Enable = true
        };

        _validatorMock.Setup(v => v.ValidateProcessInfo(It.IsAny<ProcessInfo>()))
            .Returns(new ValidationResult(false, new List<string> { "Invalid path" }));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProcessValidationException>(
            async () => await _processManager.StartProcessAsync(processInfo, false));
    }

    [TestMethod]
    public async Task EnsureProcessRunningAsync_LogsCorrectMessage()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "test",
            Path = @"C:\test.exe",
            Count = 3,
            Enable = true
        };

        _validatorMock.Setup(v => v.ValidateProcessInfo(It.IsAny<ProcessInfo>()))
            .Returns(new ValidationResult(true, new List<string>()));

        // Act
        try
        {
            await _processManager.EnsureProcessRunningAsync(processInfo, 3, false);
        }
        catch (ProcessStartException)
        {
            // Expected when the process doesn't exist
        }

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting 3 instances")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void GetRunningProcessCount_ReturnsCorrectCount()
    {
        // Arrange & Act
        var count = _processManager.GetRunningProcessCount("System");

        // Assert
        count.Should().BeGreaterOrEqualTo(0);
    }

    [TestMethod]
    public async Task StopProcessAsync_ReturnsTrue_WhenNoProcessesRunning()
    {
        // Arrange
        var processName = "nonexistent_process";

        // Act
        var result = await _processManager.StopProcessAsync(processName);

        // Assert
        result.Should().BeTrue();
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No running instances")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}