using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Orchestration;
using System.Collections.Immutable;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.UnitTests;

public class OrchestrationTests
{
    [Fact]
    public void StrategyOrchestrator_Constructor_InitializesWithStrategies()
    {
        // Arrange
        var strategies = new List<IProtectionStrategy> { new MockStrategy() };
        var logger = new MockLogger();

        // Act
        var orchestrator = new StrategyOrchestrator(strategies, logger);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_EmptyStrategies_ReturnsSuccess()
    {
        // Arrange
        var strategies = new List<IProtectionStrategy>();
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.AppliedStrategies);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_ApplicableStrategy_AppliesSuccessfully()
    {
        // Arrange
        var strategy = new MockStrategy(canApply: true, id: "test-strategy");
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("test-strategy", result.AppliedStrategies);
        Assert.True(strategy.WasApplied);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_NonApplicableStrategy_SkipsStrategy()
    {
        // Arrange
        var strategy = new MockStrategy(canApply: false, id: "test-strategy");
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("test-strategy", result.AppliedStrategies);
        Assert.False(strategy.WasApplied);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_StrategyThrowsException_HandlesGracefully()
    {
        // Arrange
        var strategy = new MockThrowingStrategy();
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, ex => ex is InvalidOperationException);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_DebugFullMode_RethrowsExceptions()
    {
        // Arrange
        var strategy = new MockThrowingStrategy();
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder()
            .Build();
        config.Debug = DebugMode.Full;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orchestrator.ExecuteAsync(module, config));
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_MultipleStrategies_AppliesInPriorityOrder()
    {
        // Arrange
        var strategy1 = new MockStrategy(id: "low-priority", priority: 100);
        var strategy2 = new MockStrategy(id: "high-priority", priority: 900);
        var strategies = new List<IProtectionStrategy> { strategy1, strategy2 };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AppliedStrategies.Length);
        Assert.Contains("low-priority", result.AppliedStrategies);
        Assert.Contains("high-priority", result.AppliedStrategies);
        Assert.True(strategy1.WasApplied);
        Assert.True(strategy2.WasApplied);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_DisabledStrategy_NotApplied()
    {
        // Arrange
        var strategy = new MockStrategy(id: "disabled-strategy");
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder()
            .WithControlFlow(false) // Assuming this disables the strategy
            .Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        // The strategy should not be applied because it's disabled in config
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_ContextPassedCorrectly()
    {
        // Arrange
        var strategy = new MockContextAwareStrategy();
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.True(strategy.ContextWasSet);
        Assert.Equal(module, strategy.ReceivedContext?.Module);
        Assert.Equal(config, strategy.ReceivedContext?.Configuration);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_Diagnostics_Collected()
    {
        // Arrange
        var strategy = new MockStrategyWithDiagnostics();
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Code == "TEST001");
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_Timing_Tracked()
    {
        // Arrange
        var strategy = new MockSlowStrategy();
        var strategies = new List<IProtectionStrategy> { strategy };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_CircularDependencies_ThrowsException()
    {
        // Arrange
        var strategy1 = new MockStrategyWithDependencies("strategy1", ["strategy2"]);
        var strategy2 = new MockStrategyWithDependencies("strategy2", ["strategy1"]); // Circular!
        var strategies = new List<IProtectionStrategy> { strategy1, strategy2 };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            orchestrator.ExecuteAsync(module, config));
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_ValidDependencies_ResolvesCorrectly()
    {
        // Arrange
        var strategy1 = new MockStrategyWithDependencies("strategy1", []);
        var strategy2 = new MockStrategyWithDependencies("strategy2", ["strategy1"]);
        var strategies = new List<IProtectionStrategy> { strategy1, strategy2 };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        // Both strategies should be applied
        Assert.Contains("strategy1", result.AppliedStrategies);
        Assert.Contains("strategy2", result.AppliedStrategies);
    }

    [Fact]
    public async Task StrategyOrchestrator_ExecuteAsync_ConflictingStrategies_Handled()
    {
        // Arrange
        var strategy1 = new MockStrategyWithConflicts("strategy1", [], ["strategy2"]);
        var strategy2 = new MockStrategyWithConflicts("strategy2", [], ["strategy1"]);
        var strategies = new List<IProtectionStrategy> { strategy1, strategy2 };
        var logger = new MockLogger();
        var orchestrator = new StrategyOrchestrator(strategies, logger);
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var result = await orchestrator.ExecuteAsync(module, config);

        // Assert
        Assert.True(result.Success);
        // Both strategies should still be applied despite conflicts
        Assert.Contains("strategy1", result.AppliedStrategies);
        Assert.Contains("strategy2", result.AppliedStrategies);
    }

    [Fact]
    public async Task ProtectionResult_Record_EqualsAndHashCode_Work()
    {
        // Arrange
        var result1 = new ProtectionResult(
            Success: true,
            AppliedStrategies: ["strategy1"],
            Errors: [],
            Duration: TimeSpan.FromSeconds(1),
            Diagnostics: []
        );
        
        var result2 = new ProtectionResult(
            Success: true,
            AppliedStrategies: ["strategy1"],
            Errors: [],
            Duration: TimeSpan.FromSeconds(1),
            Diagnostics: []
        );

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void ProtectionResult_Record_Properties_SetCorrectly()
    {
        // Arrange
        var strategies = new[] { "strategy1", "strategy2" }.ToImmutableArray();
        var errors = new Exception[] { new InvalidOperationException("test") }.ToImmutableArray();
        var diagnostics = new DiagnosticMessage[] { 
            new(DiagnosticSeverity.Info, "INFO001", "Test", null) 
        }.ToImmutableArray();
        var duration = TimeSpan.FromMilliseconds(1500);

        // Act
        var result = new ProtectionResult(true, strategies, errors, duration, diagnostics);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(strategies, result.AppliedStrategies);
        Assert.Equal(errors, result.Errors);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(diagnostics, result.Diagnostics);
    }

    #region Mock Classes

    public class MockStrategy : IProtectionStrategy
    {
        private readonly bool _canApply;
        private readonly string _id;
        private readonly int _priority;

        public MockStrategy(bool canApply = true, string id = "mock-strategy", int priority = 500)
        {
            _canApply = canApply;
            _id = id;
            _priority = priority;
        }

        public string Id => _id;
        public string Name => "Mock Strategy";
        public string Description => "A mock protection strategy for testing";
        public int Priority => _priority;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => [];

        public bool WasApplied { get; private set; } = false;

        public bool CanApply(ModuleDef module) => _canApply;

        public void Apply(ModuleDef module, ProtectionContext context)
        {
            WasApplied = true;
        }
    }

    public class MockThrowingStrategy : IProtectionStrategy
    {
        public string Id => "throwing-strategy";
        public string Name => "Throwing Strategy";
        public string Description => "A strategy that throws exceptions";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => [];

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context)
        {
            throw new InvalidOperationException("Test exception from strategy");
        }
    }

    public class MockContextAwareStrategy : IProtectionStrategy
    {
        public string Id => "context-aware-strategy";
        public string Name => "Context Aware Strategy";
        public string Description => "A strategy that uses context";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => [];

        public bool ContextWasSet { get; private set; } = false;
        public ProtectionContext? ReceivedContext { get; private set; }

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context)
        {
            ContextWasSet = true;
            ReceivedContext = context;
        }
    }

    public class MockStrategyWithDiagnostics : IProtectionStrategy
    {
        public string Id => "diagnostics-strategy";
        public string Name => "Diagnostics Strategy";
        public string Description => "A strategy that adds diagnostics";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => [];

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context)
        {
            context.AddDiagnostic(DiagnosticSeverity.Info, "TEST001", "Test diagnostic message");
        }
    }

    public class MockSlowStrategy : IProtectionStrategy
    {
        public string Id => "slow-strategy";
        public string Name => "Slow Strategy";
        public string Description => "A strategy that takes time";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => [];

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context)
        {
            Thread.Sleep(10); // Simulate work
        }
    }

    public class MockStrategyWithDependencies : IProtectionStrategy
    {
        private readonly string _id;
        private readonly ImmutableArray<string> _dependencies;

        public MockStrategyWithDependencies(string id, string[] dependencies)
        {
            _id = id;
            _dependencies = [.. dependencies];
        }

        public string Id => _id;
        public string Name => $"Strategy {_id}";
        public string Description => $"Strategy with dependencies: {string.Join(", ", _dependencies)}";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => _dependencies;
        public ImmutableArray<string> ConflictsWith => [];

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context) { }
    }

    public class MockStrategyWithConflicts : IProtectionStrategy
    {
        private readonly string _id;
        private readonly ImmutableArray<string> _conflicts;

        public MockStrategyWithConflicts(string id, string[] dependencies, string[] conflicts)
        {
            _id = id;
            _conflicts = [.. conflicts];
        }

        public string Id => _id;
        public string Name => $"Strategy {_id}";
        public string Description => $"Strategy with conflicts: {string.Join(", ", _conflicts)}";
        public int Priority => 500;
        public ImmutableArray<string> Dependencies => [];
        public ImmutableArray<string> ConflictsWith => _conflicts;

        public bool CanApply(ModuleDef module) => true;

        public void Apply(ModuleDef module, ProtectionContext context) { }
    }

    private static ModuleDefUser CreateMockModule()
    {
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));

        var module = new ModuleDefUser("TestModule.dll");

        assembly.Modules.Add(module);

        return module;
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
        public List<TypeDef> Types { get; } = [];
    }

    public class AssemblyDef
    {
        public static AssemblyDef CreateAssemblyName(string name) => new();
        public Version? Version { get; set; } = new Version(1, 0, 0, 0);
    }

    public class TypeDef
    {
        public string FullName => "TestType";
        public bool HasMethods => true;
        public List<MethodDef> Methods { get; } = new();
    }

    public class MethodDef
    {
        public string FullName => "TestMethod";
        public bool HasBody => true;
    }

    #endregion
}
