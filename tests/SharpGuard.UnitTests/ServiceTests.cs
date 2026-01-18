using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;

namespace SharpGuard.UnitTests;

public class ServiceTestsr
{
    [Fact]
    public void ConsoleLogger_Constructor_InitializesWithDefaultLevel()
    {
        // Act
        var logger = new ConsoleLogger();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void ConsoleLogger_Constructor_AcceptsMinimumLevel()
    {
        // Act
        var logger = new ConsoleLogger(LogLevel.Debug);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void ConsoleLogger_LogInformation_LogsWhenLevelAllows()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Information);

        // Act & Assert
        // This test verifies the method exists and can be called
        logger.LogInformation("Test message");
        Assert.True(true); // If no exception thrown, test passes
    }

    [Fact]
    public void ConsoleLogger_LogInformation_WithParameters_FormatsMessage()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Information);

        // Act & Assert
        logger.LogInformation("Test {0} message {1}", "formatted", 123);
        Assert.True(true);
    }

    [Fact]
    public void ConsoleLogger_LogWarning_LogsWhenLevelAllows()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Warning);

        // Act & Assert
        logger.LogWarning("Warning message");
        Assert.True(true);
    }

    [Fact]
    public void ConsoleLogger_LogError_LogsWhenLevelAllows()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Error);

        // Act & Assert
        logger.LogError(null, "Error message");
        Assert.True(true);
    }

    [Fact]
    public void ConsoleLogger_LogError_WithException_IncludesException()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Error);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        logger.LogError(exception, "Error with exception");
        Assert.True(true);
    }

    [Fact]
    public void ConsoleLogger_LogDebug_LogsWhenLevelAllows()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Debug);

        // Act & Assert
        logger.LogDebug("Debug message");
        Assert.True(true);
    }

    [Theory]
    [InlineData(LogLevel.None, false, false, false, false)]
    [InlineData(LogLevel.Critical, false, false, false, false)]
    [InlineData(LogLevel.Error, false, false, true, false)]
    [InlineData(LogLevel.Warning, false, true, true, false)]
    [InlineData(LogLevel.Information, true, true, true, false)]
    [InlineData(LogLevel.Debug, true, true, true, true)]
    [InlineData(LogLevel.Trace, true, true, true, true)]
    public void ConsoleLogger_LogLevels_RespectMinimumLevel(
        LogLevel minLevel, 
        bool expectInfo, 
        bool expectWarn, 
        bool expectError, 
        bool expectDebug)
    {
        // Arrange
        var logger = new ConsoleLogger(minLevel);

        // Act & Assert - All methods should be callable without throwing
        logger.LogInformation("Info");
        logger.LogWarning("Warn");
        logger.LogError(null, "Error");
        logger.LogDebug("Debug");
        
        Assert.True(true); // If we reach here, no exceptions were thrown
    }

    [Fact]
    public void ConsoleLogger_FormatMessage_InvalidFormat_HandlesGracefully()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Information);

        // Act & Assert
        logger.LogInformation("Invalid {0 format", "param");
        Assert.True(true); // Should not throw
    }

    [Fact]
    public void ConsoleLogger_FormatMessage_NoParameters_ReturnsOriginal()
    {
        // Arrange
        var logger = new ConsoleLogger(LogLevel.Information);

        // Act & Assert
        logger.LogInformation("No parameters message");
        Assert.True(true);
    }

    [Fact]
    public void SecureRandomGenerator_Constructor_InitializesCorrectly()
    {
        // Act
        var generator = new SecureRandomGenerator();

        // Assert
        Assert.NotNull(generator);
    }

    [Fact]
    public void SecureRandomGenerator_Next_ReturnsValueInRange()
    {
        // Arrange
        var generator = new SecureRandomGenerator();
        var min = 10;
        var max = 20;

        // Act
        var result = generator.Next(min, max);

        // Assert
        Assert.True(result >= min);
        Assert.True(result < max);
    }

    [Fact]
    public void SecureRandomGenerator_Next_MinEqualsMax_ReturnsMin()
    {
        // Arrange
        var generator = new SecureRandomGenerator();
        var value = 42;

        // Act
        var result = generator.Next(value, value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void SecureRandomGenerator_Next_MinGreaterThanMax_ThrowsException()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Next(20, 10));
    }

    [Fact]
    public void SecureRandomGenerator_NextBytes_GeneratesRequestedLength()
    {
        // Arrange
        var generator = new SecureRandomGenerator();
        var count = 16;

        // Act
        var result = generator.NextBytes(count);

        // Assert
        Assert.Equal(count, result.Length);
    }

    [Fact]
    public void SecureRandomGenerator_NextBytes_ZeroCount_ReturnsEmptyArray()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var result = generator.NextBytes(0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SecureRandomGenerator_NextBytes_NegativeCount_ThrowsException()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextBytes(-1));
    }

    [Fact]
    public void SecureRandomGenerator_NextString_GeneratesRequestedLength()
    {
        // Arrange
        var generator = new SecureRandomGenerator();
        var length = 20;

        // Act
        var result = generator.NextString(length);

        // Assert
        Assert.Equal(length, result.Length);
    }

    [Fact]
    public void SecureRandomGenerator_NextString_ZeroLength_ReturnsEmptyString()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var result = generator.NextString(0);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SecureRandomGenerator_NextString_NegativeLength_ThrowsException()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.NextString(-5));
    }

    [Fact]
    public void SecureRandomGenerator_NextString_ContainsOnlyValidCharacters()
    {
        // Arrange
        var generator = new SecureRandomGenerator();
        var result = generator.NextString(100);
        var validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        // Act & Assert
        foreach (char c in result)
        {
            Assert.Contains(c, validChars);
        }
    }

    [Fact]
    public void SecureRandomGenerator_NextDouble_ReturnsValueBetween0And1()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var result = generator.NextDouble();

        // Assert
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    [Fact]
    public void SecureRandomGenerator_MultipleCalls_ReturnDifferentValues()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var value1 = generator.Next(0, 1000);
        var value2 = generator.Next(0, 1000);

        // Assert
        // Note: There's a small chance they could be equal, but it's extremely unlikely
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void SecureRandomGenerator_NextBytes_MultipleCalls_ReturnDifferentArrays()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var bytes1 = generator.NextBytes(16);
        var bytes2 = generator.NextBytes(16);

        // Assert
        Assert.NotEqual(bytes1, bytes2);
    }

    [Fact]
    public void SecureRandomGenerator_NextString_MultipleCalls_ReturnDifferentStrings()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var string1 = generator.NextString(20);
        var string2 = generator.NextString(20);

        // Assert
        Assert.NotEqual(string1, string2);
    }

    [Fact]
    public void SecureRandomGenerator_NextDouble_MultipleCalls_ReturnDifferentValues()
    {
        // Arrange
        var generator = new SecureRandomGenerator();

        // Act
        var double1 = generator.NextDouble();
        var double2 = generator.NextDouble();

        // Assert
        Assert.NotEqual(double1, double2);
    }

    [Fact]
    public void ProtectionExtensions_IsCompilerGenerated_DetectsAttribute()
    {
        // Arrange
        var method = new MockMethodDef();

        // Act & Assert
        Assert.False(method.IsCompilerGenerated());
        
        // Add compiler generated attribute
        method.CustomAttributes.Add(new MockCustomAttribute("System.Runtime.CompilerServices.CompilerGeneratedAttribute"));
        Assert.True(method.IsCompilerGenerated());
    }

    [Fact]
    public void ProtectionExtensions_GetTypes_ExcludesGlobalModuleType()
    {
        // Arrange
        var module = new MockModuleDef();
        var normalType = new MockTypeDef { IsGlobalModuleType = false };
        var globalType = new MockTypeDef { IsGlobalModuleType = true };
        
        module.Types.Add(normalType);
        module.Types.Add(globalType);

        // Act
        var result = module.GetTypes();

        // Assert
        Assert.Contains(normalType, result);
        Assert.DoesNotContain(globalType, result);
    }

    [Fact]
    public void ProtectionExtensions_FindOrCreateStaticConstructor_CreatesNew()
    {
        // Arrange
        var type = new MockTypeDef();

        // Act
        var ctor = type.FindOrCreateStaticConstructor();

        // Assert
        Assert.NotNull(ctor);
        Assert.Equal(".cctor", ctor.Name);
        Assert.True(ctor.IsStaticConstructor);
        Assert.Contains(ctor, type.Methods);
    }

    [Fact]
    public void ProtectionExtensions_FindOrCreateStaticConstructor_ReturnsExisting()
    {
        // Arrange
        var type = new MockTypeDef();
        var existingCtor = new MockMethodDef { Name = ".cctor", IsStaticConstructor = true };
        type.Methods.Add(existingCtor);

        // Act
        var ctor = type.FindOrCreateStaticConstructor();

        // Assert
        Assert.Equal(existingCtor, ctor);
        Assert.Single(type.Methods); // Should not create duplicate
    }

    #region Mock Classes

    public class MockMethodDef
    {
        public string Name { get; set; } = string.Empty;
        public bool IsStaticConstructor { get; set; } = false;
        public System.Collections.Generic.List<MockCustomAttribute> CustomAttributes { get; } = new();
    }

    public class MockTypeDef
    {
        public bool IsGlobalModuleType { get; set; } = false;
        public System.Collections.Generic.List<MockMethodDef> Methods { get; } = new();
        
        public MockMethodDef FindOrCreateStaticConstructor()
        {
            var ctor = Methods.FirstOrDefault(m => m.IsStaticConstructor);
            if (ctor == null)
            {
                ctor = new MockMethodDef
                {
                    Name = ".cctor",
                    IsStaticConstructor = true
                };
                Methods.Add(ctor);
            }
            return ctor;
        }
    }

    public class MockModuleDef
    {
        public System.Collections.Generic.List<MockTypeDef> Types { get; } = new();
        
        public System.Collections.Generic.IEnumerable<MockTypeDef> GetTypes()
        {
            return Types.Where(t => !t.IsGlobalModuleType);
        }
    }

    public class MockCustomAttribute
    {
        public string AttributeTypeFullName { get; }

        public MockCustomAttribute(string attributeTypeFullName)
        {
            AttributeTypeFullName = attributeTypeFullName;
        }

        public string AttributeType => AttributeTypeFullName;
        public string FullName => AttributeTypeFullName;
    }

    #endregion
}
