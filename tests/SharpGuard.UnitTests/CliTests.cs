using Xunit;
using SharpGuard.CLI;

namespace SharpGuard.UnitTests;

public class CliTests
{
    [Fact]
    public void Arguments_Parse_WithInputPath_ReturnsValidArguments()
    {
        // Arrange
        var args = new[] { "test.exe" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.exe", result.InputPath);
        Assert.EndsWith("_protected.exe", result.OutputPath);
    }

    [Fact]
    public void Arguments_Parse_WithExplicitOutputPath_SetsOutputPath()
    {
        // Arrange
        var args = new[] { "-i", "input.dll", "-o", "output.dll" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("input.dll", result.InputPath);
        Assert.Equal("output.dll", result.OutputPath);
    }

    [Fact]
    public void Arguments_Parse_WithConfigPath_SetsConfigPath()
    {
        // Arrange
        var args = new[] { "input.exe", "-c", "config.xml" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("config.xml", result.ConfigPath);
    }

    [Fact]
    public void Arguments_Parse_WithLevel_SetsLevel()
    {
        // Arrange
        var args = new[] { "input.exe", "-l", "Aggressive" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("Aggressive", result.Level);
    }

    [Fact]
    public void Arguments_Parse_WithDisableFlags_SetsFlags()
    {
        // Arrange
        var args = new[] { 
            "input.exe", 
            "--no-renaming", 
            "--no-stringenc", 
            "--no-controlflow", 
            "--no-antidebug" 
        };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.True(result.DisableRenaming);
        Assert.True(result.DisableStringEncryption);
        Assert.True(result.DisableControlFlow);
        Assert.True(result.DisableAntiDebugging);
    }

    [Fact]
    public void Arguments_Parse_WithLongFormOptions_WorksCorrectly()
    {
        // Arrange
        var args = new[] { 
            "--input", "test.dll", 
            "--output", "out.dll", 
            "--config", "cfg.xml", 
            "--level", "Balanced" 
        };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("test.dll", result.InputPath);
        Assert.Equal("out.dll", result.OutputPath);
        Assert.Equal("cfg.xml", result.ConfigPath);
        Assert.Equal("Balanced", result.Level);
    }

    [Fact]
    public void Arguments_Parse_WithMixedCaseOptions_Works()
    {
        // Arrange
        var args = new[] { "-I", "test.exe", "-O", "output.exe" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("test.exe", result.InputPath);
        Assert.Equal("output.exe", result.OutputPath);
    }

    [Fact]
    public void Arguments_Parse_EmptyArgs_ReturnsNull()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Arguments_Parse_InvalidOption_IgnoresUnknownOptions()
    {
        // Arrange
        var args = new[] { "input.exe", "--unknown-option", "value" };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal("input.exe", result.InputPath);
    }

    [Theory]
    [InlineData("-i")]
    [InlineData("--input")]
    public void Arguments_Parse_InputOptions_RequireFollowingValue(string option)
    {
        // Arrange
        var args = new[] { option };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Null(result.InputPath);
    }

    [Theory]
    [InlineData("None", 0)]
    [InlineData("Minimal", 1)]
    [InlineData("Balanced", 2)]
    [InlineData("Aggressive", 3)]
    public void Arguments_Parse_LevelOptions_AcceptsAllValidLevels(string level, int expectedIndex)
    {
        // Arrange
        var args = new[] { "input.exe", "-l", level };

        // Act
        var result = Arguments.Parse(args);

        // Assert
        Assert.Equal(level, result.Level);
    }

    [Fact]
    public void Arguments_Parse_OutputPathGeneration_HandlesVariousExtensions()
    {
        // Arrange
        var testCases = new[]
        {
            ("test.exe", "test_protected.exe"),
            ("library.dll", "library_protected.dll"),
            ("app.exe.config", "app.exe_protected.config")
        };

        foreach (var (input, expected) in testCases)
        {
            // Act
            var args = new[] { input };
            var result = Arguments.Parse(args);

            // Assert
            Assert.Equal(expected, Path.GetFileName(result.OutputPath));
        }
    }
}
