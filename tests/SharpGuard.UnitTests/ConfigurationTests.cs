using SharpGuard.Core.Configuration;

namespace SharpGuard.UnitTests;

public class ConfigurationTests
{
    [Fact]
    public void ProtectionConfiguration_DefaultValues_AreCorrect()
    {
        // Act
        var config = new ProtectionConfiguration();

        // Assert
        Assert.True(config.EnableControlFlowObfuscation);
        Assert.True(config.EnableStringEncryption);
        Assert.True(config.EnableAntiDebugging);
        Assert.True(config.EnableAntiTampering);
        Assert.True(config.EnableRenaming);
        Assert.False(config.EnableWatermarking);
        Assert.False(config.EnableVirtualization);
        Assert.False(config.EnableMutation);
        Assert.True(config.EnableConstantsEncoding);
        Assert.True(config.EnableResourcesProtection);
        Assert.True(config.EnableCallIndirection);
        Assert.False(config.EnableJunkCodeInsertion);
        Assert.Equal(OptimizationLevel.Balanced, config.Optimization);
        Assert.Equal(DebugMode.None, config.Debug);
        Assert.False(config.PreserveDebugSymbols);
        Assert.True(config.PreserveCustomAttributes);
        Assert.True(config.PreservePublicApi);
        Assert.Equal(LogLevel.Information, config.MinimumLogLevel);
        Assert.Empty(config.OutputPath);
        Assert.Empty(config.KeyContainer);
        Assert.Null(config.StrongNameKey);
        Assert.Equal(Environment.ProcessorCount, config.ParallelismLevel);
        Assert.True(config.EnableLogging);
        Assert.Empty(config.ExcludedNamespaces);
        Assert.Empty(config.ExcludedTypes);
        Assert.Empty(config.ExcludedMethods);
        Assert.NotNull(config.CustomSettings);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_CreateBuilder_ReturnsNewBuilder()
    {
        // Act
        var builder = ProtectionConfiguration.CreateBuilder();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithControlFlow_EnablesControlFlow()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .Build();

        // Assert
        Assert.True(config.EnableControlFlowObfuscation);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithControlFlow_Disabled_SetsFalse()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow(false)
            .Build();

        // Assert
        Assert.False(config.EnableControlFlowObfuscation);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithStringEncryption_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithStringEncryption()
            .Build();

        // Assert
        Assert.True(config.EnableStringEncryption);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithAntiDebugging_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithAntiDebugging()
            .Build();

        // Assert
        Assert.True(config.EnableAntiDebugging);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithAntiTampering_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithAntiTampering()
            .Build();

        // Assert
        Assert.True(config.EnableAntiTampering);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithRenaming_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithRenaming()
            .Build();

        // Assert
        Assert.True(config.EnableRenaming);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithVirtualization_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithVirtualization()
            .Build();

        // Assert
        Assert.True(config.EnableVirtualization);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_WithMutation_Works()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithMutation()
            .Build();

        // Assert
        Assert.True(config.EnableMutation);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_ExcludeNamespace_AddsToExcludedNamespaces()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .ExcludeNamespace("System", "Microsoft")
            .Build();

        // Assert
        Assert.Contains("System", config.ExcludedNamespaces);
        Assert.Contains("Microsoft", config.ExcludedNamespaces);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_ExcludeType_AddsToExcludedTypes()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .ExcludeType("System.Object", "System.String")
            .Build();

        // Assert
        Assert.Contains("System.Object", config.ExcludedTypes);
        Assert.Contains("System.String", config.ExcludedTypes);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_Optimize_SetsOptimizationLevel()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .Optimize(OptimizationLevel.Aggressive)
            .Build();

        // Assert
        Assert.Equal(OptimizationLevel.Aggressive, config.Optimization);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_SetOutputPath_SetsPath()
    {
        // Arrange
        var outputPath = @"C:\temp\output.exe";

        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .SetOutputPath(outputPath)
            .Build();

        // Assert
        Assert.Equal(outputPath, config.OutputPath);
    }

    [Fact]
    public void ProtectionConfigurationBuilder_FluentInterface_AllowsChaining()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .WithStringEncryption()
            .WithAntiDebugging()
            .WithRenaming()
            .ExcludeNamespace("System")
            .ExcludeType("System.Object")
            .Optimize(OptimizationLevel.Aggressive)
            .SetOutputPath("output.exe")
            .Build();

        // Assert
        Assert.True(config.EnableControlFlowObfuscation);
        Assert.True(config.EnableStringEncryption);
        Assert.True(config.EnableAntiDebugging);
        Assert.True(config.EnableRenaming);
        Assert.Contains("System", config.ExcludedNamespaces);
        Assert.Contains("System.Object", config.ExcludedTypes);
        Assert.Equal(OptimizationLevel.Aggressive, config.Optimization);
        Assert.Equal("output.exe", config.OutputPath);
    }

    [Theory]
    [InlineData(OptimizationLevel.None)]
    [InlineData(OptimizationLevel.Minimal)]
    [InlineData(OptimizationLevel.Balanced)]
    [InlineData(OptimizationLevel.Aggressive)]
    public void OptimizationLevel_Enum_HasExpectedValues(OptimizationLevel level)
    {
        // Assert
        Assert.True(Enum.IsDefined(level));
    }

    [Theory]
    [InlineData(DebugMode.None)]
    [InlineData(DebugMode.SymbolsOnly)]
    [InlineData(DebugMode.Full)]
    public void DebugMode_Enum_HasExpectedValues(DebugMode mode)
    {
        // Assert
        Assert.True(Enum.IsDefined(mode));
    }

    [Theory]
    [InlineData(LogLevel.None)]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Trace)]
    public void LogLevel_Enum_HasExpectedValues(LogLevel level)
    {
        // Assert
        Assert.True(Enum.IsDefined(level));
    }

    [Fact]
    public void RenamingOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new RenamingOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(RenamingMode.Aggressive, options.Mode);
        Assert.False(options.RenamePublicMembers);
        Assert.True(options.RenameEnumMembers);
        Assert.True(options.RenameProperties);
        Assert.True(options.RenameEvents);
        Assert.True(options.RenameFields);
        Assert.False(options.FlattenNamespaces);
        Assert.Equal("_", options.NamespacePrefix);
        Assert.False(options.GenerateMappingFile);
    }

    [Theory]
    [InlineData(RenamingMode.None)]
    [InlineData(RenamingMode.Light)]
    [InlineData(RenamingMode.Normal)]
    [InlineData(RenamingMode.Aggressive)]
    public void RenamingMode_Enum_HasExpectedValues(RenamingMode mode)
    {
        // Assert
        Assert.True(Enum.IsDefined(mode));
    }

    [Fact]
    public void ControlFlowOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new ControlFlowOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(ControlFlowMode.Normal, options.Mode);
        Assert.Equal(10, options.ComplexityThreshold);
        Assert.True(options.InsertJunkBlocks);
        Assert.False(options.SplitMethods);
    }

    [Theory]
    [InlineData(ControlFlowMode.None)]
    [InlineData(ControlFlowMode.Light)]
    [InlineData(ControlFlowMode.Normal)]
    [InlineData(ControlFlowMode.Heavy)]
    [InlineData(ControlFlowMode.Extreme)]
    public void ControlFlowMode_Enum_HasExpectedValues(ControlFlowMode mode)
    {
        // Assert
        Assert.True(Enum.IsDefined(mode));
    }

    [Fact]
    public void EncryptionOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new EncryptionOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(EncryptionAlgorithm.AES, options.Algorithm);
        Assert.True(options.EncryptMethods);
        Assert.True(options.EncryptStrings);
        Assert.True(options.EncryptResources);
        Assert.True(options.DynamicDecryption);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.AES)]
    [InlineData(EncryptionAlgorithm.RSA)]
    [InlineData(EncryptionAlgorithm.ChaCha20)]
    [InlineData(EncryptionAlgorithm.Custom)]
    public void EncryptionAlgorithm_Enum_HasExpectedValues(EncryptionAlgorithm algorithm)
    {
        // Assert
        Assert.True(Enum.IsDefined(algorithm));
    }

    [Fact]
    public void AntiTamperOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new AntiTamperOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(AntiTamperMode.Normal, options.Mode);
        Assert.True(options.ValidateChecksum);
        Assert.True(options.ValidateSignature);
        Assert.False(options.CorruptOnTamper);
    }

    [Theory]
    [InlineData(AntiTamperMode.None)]
    [InlineData(AntiTamperMode.Light)]
    [InlineData(AntiTamperMode.Normal)]
    [InlineData(AntiTamperMode.Heavy)]
    public void AntiTamperMode_Enum_HasExpectedValues(AntiTamperMode mode)
    {
        // Assert
        Assert.True(Enum.IsDefined(mode));
    }

    [Fact]
    public void VirtualizationOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new VirtualizationOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(0.3, options.VirtualizationPercentage);
        Assert.True(options.VirtualizeMethods);
        Assert.False(options.VirtualizeTypes);
        Assert.Equal(VirtualizationEngine.Custom, options.Engine);
    }

    [Theory]
    [InlineData(VirtualizationEngine.Custom)]
    [InlineData(VirtualizationEngine.Eazfuscator)]
    [InlineData(VirtualizationEngine.SmartAssembly)]
    public void VirtualizationEngine_Enum_HasExpectedValues(VirtualizationEngine engine)
    {
        // Assert
        Assert.True(Enum.IsDefined(engine));
    }

    [Fact]
    public void MutationOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new MutationOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(MutationStrength.Medium, options.Strength);
        Assert.True(options.MutateArithmetic);
        Assert.True(options.MutateLogic);
        Assert.True(options.MutateControlFlow);
    }

    [Theory]
    [InlineData(MutationStrength.Low)]
    [InlineData(MutationStrength.Medium)]
    [InlineData(MutationStrength.High)]
    [InlineData(MutationStrength.Extreme)]
    public void MutationStrength_Enum_HasExpectedValues(MutationStrength strength)
    {
        // Assert
        Assert.True(Enum.IsDefined(strength));
    }

    [Fact]
    public void JunkCodeOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new JunkCodeOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(JunkCodeDensity.Low, options.Density);
        Assert.True(options.InsertInMethods);
        Assert.False(options.InsertInTypes);
        Assert.True(options.ObfuscateJunkCode);
    }

    [Theory]
    [InlineData(JunkCodeDensity.None)]
    [InlineData(JunkCodeDensity.Low)]
    [InlineData(JunkCodeDensity.Medium)]
    [InlineData(JunkCodeDensity.High)]
    [InlineData(JunkCodeDensity.Extreme)]
    public void JunkCodeDensity_Enum_HasExpectedValues(JunkCodeDensity density)
    {
        // Assert
        Assert.True(Enum.IsDefined(density));
    }

    [Fact]
    public void ProtectionConfigurationBuilder_MultipleExcludeCalls_UnionResults()
    {
        // Act
        var config = ProtectionConfiguration.CreateBuilder()
            .ExcludeNamespace("System")
            .ExcludeNamespace("Microsoft")
            .ExcludeType("System.Object")
            .ExcludeType("System.String")
            .Build();

        // Assert
        Assert.Contains("System", config.ExcludedNamespaces);
        Assert.Contains("Microsoft", config.ExcludedNamespaces);
        Assert.Contains("System.Object", config.ExcludedTypes);
        Assert.Contains("System.String", config.ExcludedTypes);
    }

    [Fact]
    public void ProtectionConfiguration_CustomSettings_DictionaryWorks()
    {
        // Arrange
        var config = new ProtectionConfiguration();

        // Act
        config.CustomSettings["TestKey"] = "TestValue";
        config.CustomSettings["NumberSetting"] = 42;

        // Assert
        Assert.Equal("TestValue", config.CustomSettings["TestKey"]);
        Assert.Equal(42, config.CustomSettings["NumberSetting"]);
    }
}
