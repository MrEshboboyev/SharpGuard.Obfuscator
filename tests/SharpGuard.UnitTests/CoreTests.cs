using SharpGuard.Core;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;

namespace SharpGuard.UnitTests;

public class AdvancedProtectorTests
{
    [Fact]
    public void AdvancedProtector_Constructor_InitializesWithDefaults()
    {
        // Act
        var protector = new AdvancedProtector();

        // Assert
        Assert.NotNull(protector);
    }

    [Fact]
    public void AdvancedProtector_Constructor_AcceptsCustomDependencies()
    {
        // Arrange
        var logger = new MockLogger();
        var random = new MockRandomGenerator();

        // Act
        var protector = new AdvancedProtector(logger, random);

        // Assert
        Assert.NotNull(protector);
    }

    [Fact]
    public async Task AdvancedProtector_ProtectAsync_NullInputPath_ThrowsArgumentException()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            protector.ProtectAsync(null!, config));
    }

    [Fact]
    public async Task AdvancedProtector_ProtectAsync_EmptyInputPath_ThrowsArgumentException()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            protector.ProtectAsync("", config));
    }

    [Fact]
    public async Task AdvancedProtector_ProtectAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var protector = new AdvancedProtector();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            protector.ProtectAsync("test.exe", null!));
    }

    [Fact]
    public void AdvancedProtector_CreateDefaultConfiguration_ReturnsValidConfiguration()
    {
        // Act
        var config = AdvancedProtector.CreateDefaultConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.EnableControlFlowObfuscation);
        Assert.True(config.EnableStringEncryption);
        Assert.True(config.EnableAntiDebugging);
        Assert.True(config.EnableAntiTampering);
        Assert.True(config.EnableRenaming);
        Assert.False(config.EnableWatermarking);
        Assert.Equal(OptimizationLevel.Balanced, config.Optimization);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_ValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath("output.exe")
            .Build();

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_MissingOutputPath_ReturnsError()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Output path must be specified", result.Errors);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_ConflictingStrategies_AddsWarning()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .WithMutation()
            .SetOutputPath("output.exe")
            .Build();

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("Control flow obfuscation and mutation may conflict with each other", result.Warnings);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_HighVirtualization_AddsWarning()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .WithVirtualization()
            .SetOutputPath("output.exe")
            .Build();
        
        // Manually set high virtualization percentage
        config.Virtualization.VirtualizationPercentage = 0.8;

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("High virtualization percentage may significantly impact performance", result.Warnings);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_PublicApiWithRenaming_AddsWarning()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .WithRenaming()
            .SetOutputPath("output.exe")
            .Build();
        
        config.PreservePublicApi = true;

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("Public API preservation may reduce renaming effectiveness", result.Warnings);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AdvancedProtector_ValidateConfiguration_InvalidOutputPaths_ReturnErrors(string outputPath)
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(outputPath!)
            .Build();

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Output path must be specified", result.Errors);
    }

    [Fact]
    public void AdvancedProtector_ValidateConfiguration_MultipleIssues_ReturnsAllErrorsAndWarnings()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .WithMutation()
            .WithVirtualization()
            .Build();
        
        config.Virtualization.VirtualizationPercentage = 0.9;
        config.PreservePublicApi = true;

        // Act
        var result = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Output path must be specified", result.Errors);
        Assert.Contains("Control flow obfuscation and mutation may conflict", result.Warnings);
        Assert.Contains("High virtualization percentage", result.Warnings);
        Assert.Contains("Public API preservation may reduce renaming", result.Warnings);
    }

    [Fact]
    public async Task AdvancedProtector_ProtectAsync_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath("output.exe")
            .Build();

        // Act
        var result = await protector.ProtectAsync("nonexistent.dll", config);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, ex => ex is FileNotFoundException);
    }

    [Fact]
    public void AdvancedProtector_InitializeStrategies_ReturnsAllRequiredStrategies()
    {
        // Arrange
        var logger = new MockLogger();
        var random = new MockRandomGenerator();

        // Act - Using reflection to test private method
        var protector = new AdvancedProtector(logger, random);
        var field = typeof(AdvancedProtector).GetField("_strategies", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var strategies = field?.GetValue(protector) as System.Collections.Generic.List<SharpGuard.Core.Abstractions.IProtectionStrategy>;

        // Assert
        Assert.NotNull(strategies);
        Assert.Contains(strategies, s => s.Id == "antidebug");
        Assert.Contains(strategies, s => s.Id == "stringenc");
        Assert.Contains(strategies, s => s.Id == "controlflow");
        Assert.Contains(strategies, s => s.Id == "renaming");
    }
}

// Mock implementations for testing
public class MockLogger : ILogger
{
    public void LogInformation(string message, params object[] args) { }
    public void LogWarning(string message, params object[] args) { }
    public void LogError(Exception? exception, string message, params object[] args) { }
    public void LogDebug(string message, params object[] args) { }
}

public class MockRandomGenerator : IRandomGenerator
{
    public int Next(int min, int max) => new Random().Next(min, max);
    public byte[] NextBytes(int count) => new byte[count];
    public string NextString(int length) => new('A', length);
}
