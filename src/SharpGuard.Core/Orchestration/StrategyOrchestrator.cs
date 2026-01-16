using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using System.Collections.Immutable;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core.Orchestration;

/// <summary>
/// Orchestrates the execution of protection strategies
/// Implements Topological Sort algorithm for dependency resolution
/// </summary>
public class StrategyOrchestrator(
    IEnumerable<IProtectionStrategy> strategies, 
    ILogger logger
)
{
    private readonly ImmutableList<IProtectionStrategy> _strategies = [.. strategies.OrderBy(s => s.Priority)];

    /// <summary>
    /// Executes all applicable strategies in the correct order
    /// </summary>
    public async Task<ProtectionResult> ExecuteAsync(ModuleDef module, ProtectionConfiguration config)
    {
        var context = new ProtectionContext(module, config);
        
        // Register core services
        context.RegisterService(logger);
        context.RegisterService<IRandomGenerator>(new SecureRandomGenerator());
        context.RegisterService<IMetadataPreserver>(new MetadataPreserver(config));

        var startTime = DateTime.UtcNow;
        var appliedStrategies = new List<string>();
        var errors = new List<Exception>();

        try
        {
            // Resolve strategy execution order
            var executionOrder = ResolveExecutionOrder(config);
            
            logger.LogInformation("Starting protection with {Count} strategies", executionOrder.Count);

            foreach (var strategy in executionOrder)
            {
                if (!strategy.CanApply(module))
                {
                    logger.LogDebug("Skipping strategy {Strategy} - not applicable", strategy.Name);
                    continue;
                }

                try
                {
                    logger.LogInformation("Applying strategy: {Strategy}", strategy.Name);
                    
                    var strategyStartTime = DateTime.UtcNow;
                    strategy.Apply(module, context);
                    var duration = DateTime.UtcNow - strategyStartTime;
                    
                    context.MarkStrategyApplied(strategy.Id);
                    appliedStrategies.Add(strategy.Id);
                    
                    logger.LogInformation("Strategy {Strategy} completed in {Duration}ms", 
                        strategy.Name, duration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Strategy {Strategy} failed", strategy.Name);
                    errors.Add(ex);
                    
                    if (config.Debug == DebugMode.Full)
                    {
                        throw; // Re-throw in debug mode
                    }
                }
            }

            // Perform final optimizations
            await PerformFinalOptimizationsAsync(context, config);
            
            var totalTime = DateTime.UtcNow - startTime;
            
            return new ProtectionResult(
                Success: errors.Count == 0,
                AppliedStrategies: [.. appliedStrategies],
                Errors: [.. errors],
                Duration: totalTime,
                Diagnostics: [.. context.Diagnostics]
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Protection orchestration failed");
            errors.Add(ex);
            
            return new ProtectionResult(
                Success: false,
                AppliedStrategies: [.. appliedStrategies],
                Errors: [.. errors],
                Duration: DateTime.UtcNow - startTime,
                Diagnostics: [.. context.Diagnostics]
            );
        }
    }

    /// <summary>
    /// Resolves the execution order of strategies considering dependencies and conflicts
    /// Implements topological sorting with cycle detection
    /// </summary>
    private ImmutableList<IProtectionStrategy> ResolveExecutionOrder(ProtectionConfiguration config)
    {
        var enabledStrategies = _strategies
            .Where(s => IsStrategyEnabled(s, config))
            .ToList();

        if (enabledStrategies.Count == 0)
        {
            logger.LogWarning("No protection strategies are enabled");
            return [];
        }

        // Build dependency graph
        var graph = BuildDependencyGraph(enabledStrategies);
        
        // Detect cycles
        if (HasCycles(graph))
        {
            throw new InvalidOperationException("Circular dependencies detected in protection strategies");
        }

        // Topological sort
        var sorted = TopologicalSort(graph);
        
        logger.LogDebug("Resolved execution order: {Order}", 
            string.Join(" -> ", sorted.Select(s => s.Name)));
        
        return [.. sorted];
    }

    private static Dictionary<IProtectionStrategy, List<IProtectionStrategy>> BuildDependencyGraph(
        List<IProtectionStrategy> strategies)
    {
        var graph = new Dictionary<IProtectionStrategy, List<IProtectionStrategy>>();
        
        foreach (var strategy in strategies)
        {
            graph[strategy] = [];
            
            // Add dependencies
            foreach (var depId in strategy.Dependencies)
            {
                var dependency = strategies.FirstOrDefault(s => s.Id == depId);
                if (dependency != null)
                {
                    graph[strategy].Add(dependency);
                }
            }
            
            // Add reverse dependencies for conflicts
            foreach (var conflictId in strategy.ConflictsWith)
            {
                var conflicting = strategies.FirstOrDefault(s => s.Id == conflictId);
                if (conflicting != null && !graph[conflicting].Contains(strategy))
                {
                    graph[conflicting].Add(strategy);
                }
            }
        }
        
        return graph;
    }

    private bool HasCycles(Dictionary<IProtectionStrategy, List<IProtectionStrategy>> graph)
    {
        var visited = new HashSet<IProtectionStrategy>();
        var recursionStack = new HashSet<IProtectionStrategy>();

        bool HasCycle(IProtectionStrategy node)
        {
            if (recursionStack.Contains(node))
                return true;
                
            if (visited.Contains(node))
                return false;

            visited.Add(node);
            recursionStack.Add(node);

            foreach (var neighbor in graph[node])
            {
                if (HasCycle(neighbor))
                    return true;
            }

            recursionStack.Remove(node);
            return false;
        }

        return graph.Keys.Any(HasCycle);
    }

    private static List<IProtectionStrategy> TopologicalSort(
        Dictionary<IProtectionStrategy, List<IProtectionStrategy>> graph)
    {
        var visited = new HashSet<IProtectionStrategy>();
        var result = new List<IProtectionStrategy>();

        void Visit(IProtectionStrategy node)
        {
            if (visited.Contains(node))
                return;

            visited.Add(node);

            foreach (var dependency in graph[node])
            {
                Visit(dependency);
            }

            result.Add(node);
        }

        foreach (var node in graph.Keys.OrderByDescending(n => n.Priority))
        {
            Visit(node);
        }

        return result;
    }

    private static bool IsStrategyEnabled(IProtectionStrategy strategy, ProtectionConfiguration config)
    {
        return strategy.Id switch
        {
            "controlflow" => config.EnableControlFlowObfuscation,
            "stringenc" => config.EnableStringEncryption,
            "antidebug" => config.EnableAntiDebugging,
            "antitamper" => config.EnableAntiTampering,
            "renaming" => config.EnableRenaming,
            "watermark" => config.EnableWatermarking,
            "virtualization" => config.EnableVirtualization,
            "mutation" => config.EnableMutation,
            "constants" => config.EnableConstantsEncoding,
            "resources" => config.EnableResourcesProtection,
            "callindirect" => config.EnableCallIndirection,
            "junkcode" => config.EnableJunkCodeInsertion,
            _ => true
        };
    }

    private async Task PerformFinalOptimizationsAsync(ProtectionContext context, ProtectionConfiguration config)
    {
        if (config.Optimization == OptimizationLevel.None)
            return;

        logger.LogInformation("Performing final optimizations...");
        
        // Simplify instructions
        foreach (var type in context.Module.GetTypes())
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                method.Body.SimplifyMacros(method.Parameters);
                method.Body.OptimizeMacros();
                
                if (config.Optimization >= OptimizationLevel.Balanced)
                {
                    method.Body.SimplifyBranches();
                    method.Body.OptimizeBranches();
                }
            }
        }
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Result of protection execution
/// </summary>
public record ProtectionResult(
    bool Success,
    ImmutableArray<string> AppliedStrategies,
    ImmutableArray<Exception> Errors,
    TimeSpan Duration,
    ImmutableArray<DiagnosticMessage> Diagnostics
);
