using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;

namespace SharpGuard.UnitTests;

public class AbstractionTests
{
    [Fact]
    public void ProtectionContext_Constructor_InitializesProperties()
    {
        // Arrange
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act
        var context = new ProtectionContext(module, config);

        // Assert
        Assert.Equal(module, context.Module);
        Assert.Equal(config, context.Configuration);
        Assert.Empty(context.Diagnostics);
        Assert.Empty(context.AppliedStrategies);
    }

    [Fact]
    public void ProtectionContext_RegisterAndGetService_WorksCorrectly()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MockService();

        // Act
        context.RegisterService<IMockService>(service);
        var retrieved = context.GetService<IMockService>();

        // Assert
        Assert.Equal(service, retrieved);
    }

    [Fact]
    public void ProtectionContext_GetService_UnregisteredService_ThrowsException()
    {
        // Arrange
        var context = CreateTestContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.GetService<IMockService>());
    }

    [Fact]
    public void ProtectionContext_HasService_ReturnsCorrectValues()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MockService();

        // Act & Assert
        Assert.False(context.HasService<IMockService>());
        
        context.RegisterService<IMockService>(service);
        Assert.True(context.HasService<IMockService>());
    }

    [Fact]
    public void ProtectionContext_MarkStrategyApplied_AddsToAppliedStrategies()
    {
        // Arrange
        var context = CreateTestContext();
        var strategyId = "test-strategy";

        // Act
        context.MarkStrategyApplied(strategyId);

        // Assert
        Assert.Contains(strategyId, context.AppliedStrategies);
    }

    [Fact]
    public void ProtectionContext_MarkStrategyApplied_NullOrEmpty_ThrowsException()
    {
        // Arrange
        var context = CreateTestContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.MarkStrategyApplied(null!));
        Assert.Throws<ArgumentException>(() => context.MarkStrategyApplied(""));
    }

    [Fact]
    public void ProtectionContext_AddDiagnostic_AddsToDiagnostics()
    {
        // Arrange
        var context = CreateTestContext();
        var severity = DiagnosticSeverity.Warning;
        var code = "TEST001";
        var message = "Test message";

        // Act
        context.AddDiagnostic(severity, code, message);

        // Assert
        Assert.Single(context.Diagnostics);
        var diagnostic = context.Diagnostics[0];
        Assert.Equal(severity, diagnostic.Severity);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(message, diagnostic.Message);
    }

    [Fact]
    public void ProtectionContext_CreateChildContext_CopiesParentData()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MockService();
        context.RegisterService<IMockService>(service);
        context.MarkStrategyApplied("parent-strategy");

        // Act
        var child = context.CreateChildContext();

        // Assert
        Assert.True(child.HasService<IMockService>());
        Assert.Contains("parent-strategy", child.AppliedStrategies);
        Assert.NotSame(context, child);
    }

    [Fact]
    public void ProtectionContext_ChildContext_Modifications_DontAffectParent()
    {
        // Arrange
        var context = CreateTestContext();
        var child = context.CreateChildContext();

        // Act
        child.RegisterService<IMockService>(new MockService());
        child.MarkStrategyApplied("child-strategy");

        // Assert
        Assert.False(context.HasService<IMockService>());
        Assert.DoesNotContain("child-strategy", context.AppliedStrategies);
    }

    [Fact]
    public void DiagnosticMessage_Record_EqualsAndHashCode_Work()
    {
        // Arrange
        var diag1 = new DiagnosticMessage(DiagnosticSeverity.Error, "CODE001", "Message", null);
        var diag2 = new DiagnosticMessage(DiagnosticSeverity.Error, "CODE001", "Message", null);
        var diag3 = new DiagnosticMessage(DiagnosticSeverity.Warning, "CODE002", "Different", "data");

        // Act & Assert
        Assert.Equal(diag1, diag2);
        Assert.NotEqual(diag1, diag3);
        Assert.Equal(diag1.GetHashCode(), diag2.GetHashCode());
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Info)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Error)]
    public void DiagnosticSeverity_Enum_HasExpectedValues(DiagnosticSeverity severity)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(DiagnosticSeverity), severity));
    }

    [Fact]
    public void ProtectionContext_NullModule_ThrowsArgumentNullException()
    {
        // Arrange
        var config = ProtectionConfiguration.CreateBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProtectionContext(null!, config));
    }

    [Fact]
    public void ProtectionContext_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var module = CreateMockModule();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProtectionContext(module, null!));
    }

    [Fact]
    public void ProtectionContext_RegisterService_NullService_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateTestContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.RegisterService<IMockService>(null!));
    }

    [Fact]
    public void ProtectionContext_AddMultipleDiagnostics_AllStored()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        context.AddDiagnostic(DiagnosticSeverity.Info, "INFO001", "Info message");
        context.AddDiagnostic(DiagnosticSeverity.Warning, "WARN001", "Warning message");
        context.AddDiagnostic(DiagnosticSeverity.Error, "ERR001", "Error message");

        // Assert
        Assert.Equal(3, context.Diagnostics.Count);
        Assert.Contains(context.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);
        Assert.Contains(context.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(context.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ProtectionContext_MarkSameStrategyMultipleTimes_OnlyStoredOnce()
    {
        // Arrange
        var context = CreateTestContext();
        var strategyId = "duplicate-strategy";

        // Act
        context.MarkStrategyApplied(strategyId);
        context.MarkStrategyApplied(strategyId);
        context.MarkStrategyApplied(strategyId);

        // Assert
        Assert.Single(context.AppliedStrategies);
        Assert.Contains(strategyId, context.AppliedStrategies);
    }

    [Fact]
    public void DiagnosticMessage_WithData_PropertyStoresData()
    {
        // Arrange
        var testData = new { Property = "Value" };

        // Act
        var diagnostic = new DiagnosticMessage(DiagnosticSeverity.Info, "CODE001", "Message", testData);

        // Assert
        Assert.Equal(testData, diagnostic.Data);
    }

    #region Helper Methods and Classes

    private ProtectionContext CreateTestContext()
    {
        var module = CreateMockModule();
        var config = ProtectionConfiguration.CreateBuilder().Build();
        return new ProtectionContext(module, config);
    }

    private static ModuleDefUser CreateMockModule()
    {
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));

        var module = new ModuleDefUser("TestModule.dll");

        assembly.Modules.Add(module);

        return module;
    }

    public interface IMockService
    {
        string GetData();
    }

    public class MockService : IMockService
    {
        public string GetData() => "Mock Data";
    }

    // Minimal dnlib mock classes for testing
    public class ModuleDefMD
    {
        public AssemblyDef? Assembly { get; set; }
        public string Name => "TestModule";
    }

    public class AssemblyDef
    {
        public static AssemblyDef CreateAssemblyName(string name) => new();
        public Version? Version { get; set; } = new Version(1, 0, 0, 0);
    }

    #endregion
}
