using FluentAssertions;
using MarketAlly.ProcessMonitor.Interfaces;
using MarketAlly.ProcessMonitor.Models;
using MarketAlly.ProcessMonitor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MarketAlly.ProcessMonitor.Tests.Unit;

[TestClass]
public class ProcessSchedulerTests
{
    private Mock<ILogger<ProcessScheduler>> _loggerMock = null!;
    private Mock<IProcessManager> _processManagerMock = null!;
    private ProcessScheduler _scheduler = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProcessScheduler>>();
        _processManagerMock = new Mock<IProcessManager>();
        _scheduler = new ProcessScheduler(_loggerMock.Object, _processManagerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scheduler?.Dispose();
    }

    [TestMethod]
    public void ScheduleProcessStart_WithValidTime_SchedulesProcess()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = "C:\\test.exe",
            Time = DateTime.Now.AddHours(1).ToString("HH:mm"),
            Enable = true
        };

        // Act
        _scheduler.ScheduleProcessStart(processInfo, false);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Scheduled TestProcess to start at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void ScheduleProcessStart_WithoutTime_DoesNotSchedule()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = "C:\\test.exe",
            Time = null,
            Enable = true
        };

        // Act
        _scheduler.ScheduleProcessStart(processInfo, false);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cannot schedule process")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void ScheduleRepeatedExecution_WithValidInterval_SchedulesProcess()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = "C:\\test.exe",
            Interval = 15,
            Enable = true
        };

        _processManagerMock.Setup(m => m.GetRunningProcessCount(It.IsAny<string>()))
            .Returns(0);

        // Act
        _scheduler.ScheduleRepeatedExecution(processInfo, false);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Scheduled TestProcess to run every 15 minutes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void CancelScheduledTasks_RemovesAllTasksForProcess()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = "C:\\test.exe",
            Interval = 15,
            Enable = true
        };

        _scheduler.ScheduleRepeatedExecution(processInfo, false);

        // Act
        _scheduler.CancelScheduledTasks("TestProcess");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cancelled all scheduled tasks for TestProcess")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void Dispose_DisposesAllTimers()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = "C:\\test.exe",
            Interval = 15,
            Enable = true
        };

        _scheduler.ScheduleRepeatedExecution(processInfo, false);

        // Act
        _scheduler.Dispose();

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ProcessScheduler disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}