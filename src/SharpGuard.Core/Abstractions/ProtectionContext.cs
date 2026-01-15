using dnlib.DotNet;
using SharpGuard.Core.Configuration;

namespace SharpGuard.Core.Abstractions;

/// <summary>
/// Context containing shared state and services for protection operations
/// Implements Dependency Injection pattern
/// </summary>
public sealed class ProtectionContext
{
    private readonly Dictionary<string, object> _services = [];
    private readonly HashSet<string> _appliedStrategies = [];
    private readonly List<DiagnosticMessage> _diagnostics = [];

    /// <summary>
    /// Gets the module being protected
    /// </summary>
    public ModuleDef Module { get; }

    /// <summary>
    /// Gets the configuration for protection
    /// </summary>
    public ProtectionConfiguration Configuration { get; }

    /// <summary>
    /// Gets diagnostic messages collected during protection
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Diagnostics => _diagnostics;

    /// <summary>
    /// Gets the set of strategies that have been applied
    /// </summary>
    public IReadOnlySet<string> AppliedStrategies => _appliedStrategies;

    public ProtectionContext(ModuleDef module, ProtectionConfiguration configuration)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Registers a service in the context
    /// Implements Service Locator pattern
    /// </summary>
    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T).FullName ?? typeof(T).Name] = service 
            ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Retrieves a service from the context
    /// </summary>
    public T GetService<T>() where T : class
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        return _services.TryGetValue(key, out var service) 
            ? (T)service 
            : throw new InvalidOperationException($"Service {key} not registered");
    }

    /// <summary>
    /// Checks if a service is registered
    /// </summary>
    public bool HasService<T>() where T : class
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        return _services.ContainsKey(key);
    }

    /// <summary>
    /// Marks a strategy as applied
    /// </summary>
    public void MarkStrategyApplied(string strategyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(strategyId);
        _appliedStrategies.Add(strategyId);
    }

    /// <summary>
    /// Adds a diagnostic message
    /// </summary>
    public void AddDiagnostic(DiagnosticSeverity severity, string code, string message, object? data = null)
    {
        _diagnostics.Add(new DiagnosticMessage(severity, code, message, data));
    }

    /// <summary>
    /// Creates a child context for scoped operations
    /// Implements Prototype pattern
    /// </summary>
    public ProtectionContext CreateChildContext()
    {
        var child = new ProtectionContext(Module, Configuration);
        
        // Copy services
        foreach (var kvp in _services)
        {
            child._services[kvp.Key] = kvp.Value;
        }
        
        // Copy applied strategies
        foreach (var strategy in _appliedStrategies)
        {
            child._appliedStrategies.Add(strategy);
        }
        
        return child;
    }
}

/// <summary>
/// Represents a diagnostic message from the protection process
/// </summary>
public record DiagnosticMessage(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    object? Data);

/// <summary>
/// Severity levels for diagnostic messages
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}
