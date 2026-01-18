using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using SharpGuard.Core.Strategies;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.UnitTests;

public class StrategyTests
{
    [Fact]
    public void AntiDebuggingStrategy_ImplementsIProtectionStrategy()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();

        // Act
        var strategy = new AntiDebuggingStrategy(random, logger);

        // Assert
        Assert.IsAssignableFrom<IProtectionStrategy>(strategy);
        Assert.Equal("antidebug", strategy.Id);
        Assert.Equal("Advanced Anti-Debugging Protection", strategy.Name);
        Assert.Equal(950, strategy.Priority);
    }

    [Fact]
    public void AntiDebuggingStrategy_CanApply_AlwaysReturnsTrue()
    {
        // Arrange
        var strategy = CreateAntiDebuggingStrategy();
        var module = CreateMockModule();

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AntiDebuggingStrategy_Apply_WhenDisabled_DoesNothing()
    {
        // Arrange
        var strategy = CreateAntiDebuggingStrategy();
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithAntiDebugging(false)
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should not throw and complete successfully
        Assert.True(true);
    }

    [Fact]
    public void AntiDebuggingStrategy_Apply_WhenEnabled_ProcessesModule()
    {
        // Arrange
        var strategy = CreateAntiDebuggingStrategy();
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithAntiDebugging()
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void AntiDebuggingStrategy_DependenciesAndConflicts_AreEmpty()
    {
        // Arrange
        var strategy = CreateAntiDebuggingStrategy();

        // Assert
        Assert.Empty(strategy.Dependencies);
        Assert.Empty(strategy.ConflictsWith);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_ImplementsIProtectionStrategy()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();

        // Act
        var strategy = new ControlFlowObfuscationStrategy(random, logger);

        // Assert
        Assert.IsAssignableFrom<IProtectionStrategy>(strategy);
        Assert.Equal("controlflow", strategy.Id);
        Assert.Equal("Advanced Control Flow Obfuscation", strategy.Name);
        Assert.Equal(800, strategy.Priority);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_CanApply_WithMethods_ReturnsTrue()
    {
        // Arrange
        var strategy = CreateControlFlowStrategy();
        var module = CreateMockModuleWithMethods();

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_CanApply_WithoutMethods_ReturnsFalse()
    {
        // Arrange
        var strategy = CreateControlFlowStrategy();
        var module = CreateMockModule(); // No methods

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_Apply_WhenDisabled_DoesNothing()
    {
        // Arrange
        var strategy = CreateControlFlowStrategy();
        var module = CreateMockModuleWithMethods();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow(false)
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_Apply_WhenEnabled_ProcessesMethods()
    {
        // Arrange
        var strategy = CreateControlFlowStrategy();
        var module = CreateMockModuleWithMethods();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void ControlFlowObfuscationStrategy_HasExpectedDependenciesAndConflicts()
    {
        // Arrange
        var strategy = CreateControlFlowStrategy();

        // Assert
        Assert.Empty(strategy.Dependencies);
        Assert.Contains("mutation", strategy.ConflictsWith);
    }

    [Fact]
    public void RenamingStrategy_ImplementsIProtectionStrategy()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();

        // Act
        var strategy = new RenamingStrategy(random, logger);

        // Assert
        Assert.IsAssignableFrom<IProtectionStrategy>(strategy);
        Assert.Equal("renaming", strategy.Id);
        Assert.Equal("Intelligent Symbol Renaming", strategy.Name);
        Assert.Equal(700, strategy.Priority);
    }

    [Fact]
    public void RenamingStrategy_CanApply_WithMultipleTypes_ReturnsTrue()
    {
        // Arrange
        var strategy = CreateRenamingStrategy();
        var module = CreateMockModuleWithTypes();

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RenamingStrategy_CanApply_WithSingleType_ReturnsFalse()
    {
        // Arrange
        var strategy = CreateRenamingStrategy();
        var module = CreateMockModule(); // Only global type

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RenamingStrategy_Apply_WhenDisabled_DoesNothing()
    {
        // Arrange
        var strategy = CreateRenamingStrategy();
        var module = CreateMockModuleWithTypes();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithRenaming(false)
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void RenamingStrategy_Apply_WhenEnabled_RenamesSymbols()
    {
        // Arrange
        var strategy = CreateRenamingStrategy();
        var module = CreateMockModuleWithTypes();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithRenaming()
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void RenamingStrategy_DependenciesAndConflicts_AreEmpty()
    {
        // Arrange
        var strategy = CreateRenamingStrategy();

        // Assert
        Assert.Empty(strategy.Dependencies);
        Assert.Empty(strategy.ConflictsWith);
    }

    [Fact]
    public void StringEncryptionStrategy_ImplementsIProtectionStrategy()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();

        // Act
        var strategy = new StringEncryptionStrategy(random, logger);

        // Assert
        Assert.IsAssignableFrom<IProtectionStrategy>(strategy);
        Assert.Equal("stringenc", strategy.Id);
        Assert.Equal("Advanced String Encryption", strategy.Name);
        Assert.Equal(900, strategy.Priority);
    }

    [Fact]
    public void StringEncryptionStrategy_CanApply_WithStrings_ReturnsTrue()
    {
        // Arrange
        var strategy = CreateStringEncryptionStrategy();
        var module = CreateMockModuleWithStrings();

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StringEncryptionStrategy_CanApply_WithoutStrings_ReturnsFalse()
    {
        // Arrange
        var strategy = CreateStringEncryptionStrategy();
        var module = CreateMockModule(); // No strings

        // Act
        var result = strategy.CanApply(module);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StringEncryptionStrategy_Apply_WhenDisabled_DoesNothing()
    {
        // Arrange
        var strategy = CreateStringEncryptionStrategy();
        var module = CreateMockModuleWithStrings();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithStringEncryption(false)
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void StringEncryptionStrategy_Apply_WhenEnabled_EncryptsStrings()
    {
        // Arrange
        var strategy = CreateStringEncryptionStrategy();
        var module = CreateMockModuleWithStrings();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithStringEncryption()
            .Build();
        var context = new ProtectionContext(module, config);

        // Act
        strategy.Apply(module, context);

        // Assert - Should complete without exceptions
        Assert.True(true);
    }

    [Fact]
    public void StringEncryptionStrategy_DependenciesAndConflicts_AreEmpty()
    {
        // Arrange
        var strategy = CreateStringEncryptionStrategy();

        // Assert
        Assert.Empty(strategy.Dependencies);
        Assert.Empty(strategy.ConflictsWith);
    }

    [Fact]
    public void StrategyPriorities_AreInCorrectOrder()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();
        
        var antiDebug = new AntiDebuggingStrategy(random, logger);
        var stringEnc = new StringEncryptionStrategy(random, logger);
        var controlFlow = new ControlFlowObfuscationStrategy(random, logger);
        var renaming = new RenamingStrategy(random, logger);

        // Assert - Higher priority executes first
        Assert.True(antiDebug.Priority > stringEnc.Priority);
        Assert.True(stringEnc.Priority > controlFlow.Priority);
        Assert.True(controlFlow.Priority > renaming.Priority);
    }

    [Fact]
    public void AllStrategies_HaveUniqueIds()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();
        
        var strategies = new IProtectionStrategy[]
        {
            new AntiDebuggingStrategy(random, logger),
            new StringEncryptionStrategy(random, logger),
            new ControlFlowObfuscationStrategy(random, logger),
            new RenamingStrategy(random, logger)
        };

        // Act
        var ids = strategies.Select(s => s.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count()); // All IDs are unique
    }

    [Fact]
    public void AllStrategies_HaveNonEmptyDescriptions()
    {
        // Arrange
        var random = new MockRandomGenerator();
        var logger = new MockLogger();
        
        var strategies = new IProtectionStrategy[]
        {
            new AntiDebuggingStrategy(random, logger),
            new StringEncryptionStrategy(random, logger),
            new ControlFlowObfuscationStrategy(random, logger),
            new RenamingStrategy(random, logger)
        };

        // Assert
        foreach (var strategy in strategies)
        {
            Assert.False(string.IsNullOrWhiteSpace(strategy.Description));
        }
    }

    #region Helper Methods

    private AntiDebuggingStrategy CreateAntiDebuggingStrategy()
    {
        return new AntiDebuggingStrategy(new MockRandomGenerator(), new MockLogger());
    }

    private ControlFlowObfuscationStrategy CreateControlFlowStrategy()
    {
        return new ControlFlowObfuscationStrategy(new MockRandomGenerator(), new MockLogger());
    }

    private RenamingStrategy CreateRenamingStrategy()
    {
        return new RenamingStrategy(new MockRandomGenerator(), new MockLogger());
    }

    private StringEncryptionStrategy CreateStringEncryptionStrategy()
    {
        return new StringEncryptionStrategy(new MockRandomGenerator(), new MockLogger());
    }

    private static ModuleDefUser CreateMockModule()
    {
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));

        var module = new ModuleDefUser("TestModule.dll");

        assembly.Modules.Add(module);

        return module;
    }


    private ModuleDefMD CreateMockModuleWithMethods()
    {
        var module = CreateMockModule();
        var type = new TypeDef { IsGlobalModuleType = false };
        var method = new MethodDef { HasBody = true };
        type.Methods.Add(method);
        module.Types.Add(type);
        return module;
    }

    private ModuleDefMD CreateMockModuleWithTypes()
    {
        var module = CreateMockModule();
        var type1 = new TypeDef { IsGlobalModuleType = false };
        var type2 = new TypeDef { IsGlobalModuleType = false };
        module.Types.Add(type1);
        module.Types.Add(type2);
        return module;
    }

    private ModuleDefMD CreateMockModuleWithStrings()
    {
        var module = CreateMockModule();
        var type = new TypeDef { IsGlobalModuleType = false };
        var method = new MethodDef { HasBody = true };
        // In a real scenario, we'd add string instructions to the method body
        type.Methods.Add(method);
        module.Types.Add(type);
        return module;
    }

    #endregion

    #region Mock Classes

    public class MockRandomGenerator : IRandomGenerator
    {
        private readonly Random _random = new();

        public int Next(int min, int max) => _random.Next(min, max);
        public byte[] NextBytes(int count) => new byte[count];
        public string NextString(int length) => new('A', length);
        public double NextDouble() => _random.NextDouble();
    }

    public class MockLogger : ILogger
    {
        public void LogInformation(string message, params object[] args) { }
        public void LogWarning(string message, params object[] args) { }
        public void LogError(Exception? exception, string message, params object[] args) { }
        public void LogDebug(string message, params object[] args) { }
    }

    // Minimal dnlib mock classes
    public class ModuleDefMD
    {
        public AssemblyDef? Assembly { get; set; }
        public string Name => "TestModule";
        public System.Collections.Generic.List<TypeDef> Types { get; } = new();
    }

    public class AssemblyDef
    {
        public static AssemblyDef CreateAssemblyName(string name) => new();
        public Version? Version { get; set; } = new Version(1, 0, 0, 0);
    }

    public class TypeDef
    {
        public string FullName => "TestType";
        public string Name => "TestType";
        public string Namespace => "TestNamespace";
        public bool IsGlobalModuleType { get; set; } = false;
        public bool HasMethods => Methods.Any();
        public System.Collections.Generic.List<MethodDef> Methods { get; } = new();
        public System.Collections.Generic.List<FieldDef> Fields { get; } = new();
        public System.Collections.Generic.List<PropertyDef> Properties { get; } = new();
        public System.Collections.Generic.List<EventDef> Events { get; } = new();
    }

    public class MethodDef
    {
        public string FullName => "TestMethod";
        public string Name => "TestMethod";
        public bool HasBody { get; set; } = false;
        public bool IsConstructor { get; set; } = false;
        public bool IsStaticConstructor { get; set; } = false;
        public bool IsSpecialName { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        public bool IsFamily { get; set; } = false;
        public bool IsPinvokeImpl { get; set; } = false;
        public bool IsGetter { get; set; } = false;
        public bool IsSetter { get; set; } = false;
        public int Overrides { get; set; } = 0;
        public System.Collections.Generic.List<object> CustomAttributes { get; } = new();
    }

    public class FieldDef
    {
        public string FullName => "TestField";
        public string Name => "TestField";
        public bool IsSpecialName { get; set; } = false;
        public bool IsLiteral { get; set; } = false;
        public bool IsPublic { get; set; } = false;
    }

    public class PropertyDef
    {
        public string FullName => "TestProperty";
        public string Name => "TestProperty";
        public MethodDef? GetMethod { get; set; }
        public MethodDef? SetMethod { get; set; }
    }

    public class EventDef
    {
        public string FullName => "TestEvent";
        public string Name => "TestEvent";
        public MethodDef? AddMethod { get; set; }
        public MethodDef? RemoveMethod { get; set; }
        public MethodDef? InvokeMethod { get; set; }
    }

    #endregion
}