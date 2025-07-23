using FluentAssertions;
using MarketAlly.ProcessMonitor.Models;
using MarketAlly.ProcessMonitor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MarketAlly.ProcessMonitor.Tests.Unit;

[TestClass]
public class ProcessValidatorTests
{
    private Mock<ILogger<ProcessValidator>> _loggerMock = null!;
    private ProcessValidator _validator = null!;
    private AppSettings _appSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ProcessValidator>>();
        _appSettings = new AppSettings
        {
            EnablePathValidation = true,
            AllowedPaths = new List<string> { @"C:\Windows", @"C:\Program Files" }
        };
        _validator = new ProcessValidator(_loggerMock.Object, _appSettings);
    }

    [TestMethod]
    public void ValidateProcessPath_WithNullPath_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateProcessPath(null!);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void ValidateProcessPath_WithRelativePath_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateProcessPath("relative/path.exe");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void ValidateProcessPath_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateProcessPath(@"C:\nonexistent\file.exe");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void ValidateProcessPath_WithNonExecutableFile_ReturnsFalse()
    {
        // Act
        var result = _validator.ValidateProcessPath(@"C:\Windows\System32\drivers\etc\hosts");

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void ValidateProcessInfo_WithValidInfo_ReturnsValid()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = @"C:\Windows\System32\notepad.exe",
            Count = 1,
            Enable = true
        };

        // Act
        var result = _validator.ValidateProcessInfo(processInfo);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void ValidateProcessInfo_WithInvalidTime_ReturnsError()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = @"C:\Windows\System32\notepad.exe",
            Time = "25:00", // Invalid time
            Enable = true
        };

        // Act
        var result = _validator.ValidateProcessInfo(processInfo);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid time format"));
    }

    [TestMethod]
    public void ValidateProcessInfo_WithNegativeInterval_ReturnsError()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = @"C:\Windows\System32\notepad.exe",
            Interval = -5,
            Enable = true
        };

        // Act
        var result = _validator.ValidateProcessInfo(processInfo);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Interval must be positive"));
    }

    [TestMethod]
    public void ValidateProcessInfo_WithDangerousArguments_ReturnsError()
    {
        // Arrange
        var processInfo = new ProcessInfo
        {
            Name = "TestProcess",
            Path = @"C:\Windows\System32\notepad.exe",
            Arguments = "test; rm -rf /",
            Enable = true
        };

        // Act
        var result = _validator.ValidateProcessInfo(processInfo);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("dangerous characters"));
    }

    [TestMethod]
    public async Task CheckPermissionsAsync_WithAccessibleFile_ReturnsTrue()
    {
        // Act
        var result = await _validator.CheckPermissionsAsync(@"C:\Windows\System32\notepad.exe");

        // Assert
        result.Should().BeTrue();
    }
}