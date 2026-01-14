using System.Collections.Immutable;
using dnlib.DotNet;

namespace SharpGuard.Core.Abstractions;

/// <summary>
/// Defines a protection strategy that applies multiple transformations to assemblies
/// </summary>
public interface IProtectionStrategy
{
    /// <summary>
    /// Gets the unique identifier for this strategy
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets the human-readable name of the strategy
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of what this strategy protects against
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the priority level (higher values execute first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Determines if this strategy can be applied to the given module
    /// </summary>
    bool CanApply(ModuleDef module);
    
    /// <summary>
    /// Applies the protection strategy to the module
    /// </summary>
    void Apply(ModuleDef module, ProtectionContext context);
    
    /// <summary>
    /// Gets the dependencies this strategy requires
    /// </summary>
    ImmutableArray<string> Dependencies { get; }
    
    /// <summary>
    /// Gets the strategies this strategy conflicts with
    /// </summary>
    ImmutableArray<string> ConflictsWith { get; }
}