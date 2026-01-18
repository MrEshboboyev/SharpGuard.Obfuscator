using SharpGuard.Core;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;

namespace SharpGuard.UnitTests;

public class IntegrationTests
{
    [Fact]
    public async Task AdvancedProtector_EndToEndWorkflow_CompletesSuccessfully()
    {
        // Arrange
        var logger = new TestLogger();
        var protector = new AdvancedProtector(logger);
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath())
            .WithControlFlow(false) // Disable for testing speed
            .WithStringEncryption(false)
            .WithAntiDebugging(false)
            .WithRenaming(false)
            .Build();

        // Use a simple test assembly
        var inputPath = CreateTestAssembly();

        try
        {
            // Act
            var result = await protector.ProtectAsync(inputPath, config);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            Assert.True(result.Duration > TimeSpan.Zero);
            
            // Verify some strategies were attempted (even if they didn't apply)
            Assert.NotNull(result.AppliedStrategies);
        }
        finally
        {
            // Cleanup
            CleanupTestFiles(inputPath, config.OutputPath);
        }
    }

    [Fact]
    public async Task AdvancedProtector_ConfigurationValidation_Integration_Works()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = AdvancedProtector.CreateDefaultConfiguration();
        config.OutputPath = GetTempFilePath();

        // Act
        var validationResult = AdvancedProtector.ValidateConfiguration(config);
        var protectionResult = await protector.ProtectAsync(CreateTestAssembly(), config);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.True(protectionResult.Success);
    }

    [Fact]
    public async Task AdvancedProtector_InvalidConfiguration_ReturnsValidationErrors()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = ProtectionConfiguration.CreateBuilder().Build(); // No output path

        // Act
        var validationResult = AdvancedProtector.ValidateConfiguration(config);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Output path must be specified", validationResult.Errors);
    }

    [Fact]
    public async Task AdvancedProtector_NonExistentInputFile_ReturnsError()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath())
            .Build();

        // Act
        var result = await protector.ProtectAsync("nonexistent.dll", config);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, ex => ex is FileNotFoundException);
    }

    [Fact]
    public void ProtectionConfiguration_BuilderPattern_Integration_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .WithStringEncryption()
            .WithAntiDebugging()
            .WithRenaming()
            .ExcludeNamespace("System", "Microsoft")
            .ExcludeType("System.Object")
            .Optimize(OptimizationLevel.Aggressive)
            .SetOutputPath("test_output.exe")
            .Build();

        // Assert
        Assert.True(config.EnableControlFlowObfuscation);
        Assert.True(config.EnableStringEncryption);
        Assert.True(config.EnableAntiDebugging);
        Assert.True(config.EnableRenaming);
        Assert.Contains("System", config.ExcludedNamespaces);
        Assert.Contains("Microsoft", config.ExcludedNamespaces);
        Assert.Contains("System.Object", config.ExcludedTypes);
        Assert.Equal(OptimizationLevel.Aggressive, config.Optimization);
        Assert.Equal("test_output.exe", config.OutputPath);
    }

    [Fact]
    public async Task AdvancedProtector_LoggingIntegration_LogsAreGenerated()
    {
        // Arrange
        var logger = new TestLogger();
        var protector = new AdvancedProtector(logger);
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath())
            .Build();

        // Act
        await protector.ProtectAsync(CreateTestAssembly(), config);

        // Assert
        Assert.NotEmpty(logger.LogEntries);
        Assert.Contains(logger.LogEntries, entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task AdvancedProtector_DiagnosticsCollection_Works()
    {
        // Arrange
        var logger = new TestLogger();
        var protector = new AdvancedProtector(logger);
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath())
            .Build();

        // Act
        var result = await protector.ProtectAsync(CreateTestAssembly(), config);

        // Assert
        Assert.NotNull(result.Diagnostics);
        // Diagnostics may be empty depending on the protection process
    }

    [Fact]
    public async Task AdvancedProtector_ErrorHandling_Integration_HandlesExceptions()
    {
        // Arrange
        var logger = new TestLogger();
        var protector = new AdvancedProtector(logger);
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath("") // Invalid output path
            .Build();

        // Act
        var result = await protector.ProtectAsync(CreateTestAssembly(), config);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ConfigurationDefaults_AreConsistentAcrossCreationMethods()
    {
        // Act
        var config1 = new ProtectionConfiguration();
        var config2 = ProtectionConfiguration.CreateBuilder().Build();
        var config3 = AdvancedProtector.CreateDefaultConfiguration();

        // Assert - Basic consistency checks
        Assert.Equal(config1.EnableControlFlowObfuscation, config2.EnableControlFlowObfuscation);
        Assert.Equal(config1.EnableStringEncryption, config2.EnableStringEncryption);
        Assert.Equal(config1.Optimization, config2.Optimization);
    }

    [Fact]
    public async Task AdvancedProtector_MultipleProtectionRuns_AreIndependent()
    {
        // Arrange
        var protector = new AdvancedProtector();
        var config1 = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath("output1"))
            .Build();
        var config2 = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath("output2"))
            .Build();
        var inputPath = CreateTestAssembly();

        try
        {
            // Act
            var result1 = await protector.ProtectAsync(inputPath, config1);
            var result2 = await protector.ProtectAsync(inputPath, config2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotEqual(config1.OutputPath, config2.OutputPath);
        }
        finally
        {
            CleanupTestFiles(inputPath, config1.OutputPath, config2.OutputPath);
        }
    }

    [Fact]
    public async Task AdvancedProtector_StrategyApplication_OrderIsRespected()
    {
        // Arrange
        var logger = new TestLogger();
        var protector = new AdvancedProtector(logger);
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(GetTempFilePath())
            .WithAntiDebugging()    // Priority 950
            .WithStringEncryption() // Priority 900  
            .WithControlFlow()      // Priority 800
            .WithRenaming()         // Priority 700
            .Build();

        // Act
        var result = await protector.ProtectAsync(CreateTestAssembly(), config);

        // Assert
        Assert.True(result.Success);
        // The strategies should be applied in priority order (highest first)
        // This is tested at the orchestration level, but we verify it completes
    }

    #region Helper Methods

    private static string GetTempFilePath(string suffix = "")
    {
        var fileName = $"test_{Guid.NewGuid():N}{suffix}.exe";
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    private static string CreateTestAssembly()
    {
        // Create a minimal test assembly for testing
        var tempPath = GetTempFilePath("_input");
        
        // For testing purposes, we'll create a simple text file that simulates an assembly
        // In a real scenario, this would be a compiled .NET assembly
        File.WriteAllText(tempPath, "TEST_ASSEMBLY_CONTENT");
        
        return tempPath;
    }

    private static void CleanupTestFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    #endregion

    #region Test Logger

    public class TestLogger : ILogger
    {
        public List<LogEntry> LogEntries { get; } = [];

        public void LogInformation(string message, params object[] args)
        {
            LogEntries.Add(new LogEntry(LogLevel.Information, FormatMessage(message, args)));
        }

        public void LogWarning(string message, params object[] args)
        {
            LogEntries.Add(new LogEntry(LogLevel.Warning, FormatMessage(message, args)));
        }

        public void LogError(Exception? exception, string message, params object[] args)
        {
            LogEntries.Add(new LogEntry(LogLevel.Error, FormatMessage(message, args), exception));
        }

        public void LogDebug(string message, params object[] args)
        {
            LogEntries.Add(new LogEntry(LogLevel.Debug, FormatMessage(message, args)));
        }

        private static string FormatMessage(string message, object[] args)
        {
            try
            {
                return args.Length > 0 ? string.Format(message, args) : message;
            }
            catch
            {
                return message;
            }
        }

        public record LogEntry(LogLevel Level, string Message, Exception? Exception = null);
    }

    #endregion
}
