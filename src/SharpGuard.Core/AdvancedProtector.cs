using System.Collections.Immutable;
using System.Reflection;
using dnlib.DotNet;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Orchestration;
using SharpGuard.Core.Services;
using SharpGuard.Core.Strategies;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core;

/// <summary>
/// Main protector class implementing Facade pattern
/// Provides simplified interface to complex protection subsystem
/// </summary>
public class AdvancedProtector
{
    private readonly ILogger _logger;
    private readonly IRandomGenerator _random;
    private readonly List<IProtectionStrategy> _strategies;
    private readonly StrategyOrchestrator _orchestrator;

    public AdvancedProtector(ILogger? logger = null, IRandomGenerator? random = null)
    {
        _logger = logger ?? new ConsoleLogger();
        _random = random ?? new SecureRandomGenerator();
        
        // Initialize strategies with proper dependencies
        _strategies = InitializeStrategies();
        _orchestrator = new StrategyOrchestrator(_strategies, _logger);
    }

    /// <summary>
    /// Protects an assembly with advanced obfuscation techniques
    /// Implements Template Method pattern
    /// </summary>
    public async Task<ProtectionResult> ProtectAsync(
        string inputPath, 
        ProtectionConfiguration config)
    {
        if (string.IsNullOrEmpty(inputPath))
            throw new ArgumentException("Input path cannot be null or empty", nameof(inputPath));

        ArgumentNullException.ThrowIfNull(config);

        _logger.LogInformation("Starting advanced protection for: {Path}", inputPath);

        try
        {
            // Pre-processing phase
            var preprocessingResult = await PreprocessAsync(inputPath, config);
            if (!preprocessingResult.Success)
                return preprocessingResult;

            // Load module
            var module = LoadModule(inputPath);
            if (module == null)
            {
                return new ProtectionResult(
                    Success: false,
                    AppliedStrategies: [],
                    Errors: [new InvalidOperationException("Failed to load module")],
                    Duration: TimeSpan.Zero,
                    Diagnostics: []
                );
            }

            // Main protection phase
            var protectionResult = await _orchestrator.ExecuteAsync(module, config);

            // Post-processing phase
            if (protectionResult.Success)
            {
                var postProcessingResult = await PostProcessAsync(module, config, protectionResult);
                if (!postProcessingResult.Success)
                    return postProcessingResult;
            }

            // Save result
            if (protectionResult.Success)
            {
                await SaveModuleAsync(module, config.OutputPath);
                _logger.LogInformation("Protection completed successfully. Output saved to: {Path}", config.OutputPath);
            }

            return protectionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Protection failed");
            return new ProtectionResult(
                Success: false,
                AppliedStrategies: ImmutableArray<string>.Empty,
                Errors: ImmutableArray.Create(ex),
                Duration: TimeSpan.Zero,
                Diagnostics: ImmutableArray<DiagnosticMessage>.Empty
            );
        }
    }

    /// <summary>
    /// Creates default protection configuration
    /// Implements Factory pattern
    /// </summary>
    public static ProtectionConfiguration CreateDefaultConfiguration()
    {
        return ProtectionConfiguration.CreateBuilder()
            .WithControlFlow()
            .WithStringEncryption()
            .WithAntiDebugging()
            .WithAntiTampering()
            .WithRenaming()
            .Optimize(OptimizationLevel.Balanced)
            .Build();
    }

    /// <summary>
    /// Validates configuration for potential issues
    /// Implements Validation pattern
    /// </summary>
    public static ValidationResult ValidateConfiguration(ProtectionConfiguration config)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for conflicting strategies
        if (config.EnableControlFlowObfuscation && config.EnableMutation)
        {
            warnings.Add("Control flow obfuscation and mutation may conflict with each other");
        }

        // Check for performance implications
        if (config.EnableVirtualization && config.Virtualization.VirtualizationPercentage > 0.7)
        {
            warnings.Add("High virtualization percentage may significantly impact performance");
        }

        // Check for security trade-offs
        if (config.PreservePublicApi && config.EnableRenaming)
        {
            warnings.Add("Public API preservation may reduce renaming effectiveness");
        }

        // Validate output path
        if (string.IsNullOrEmpty(config.OutputPath))
        {
            errors.Add("Output path must be specified");
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    private List<IProtectionStrategy> InitializeStrategies()
    {
        return
        [
            new AntiDebuggingStrategy(_random, _logger),
            new StringEncryptionStrategy(_random, _logger),
            new ControlFlowObfuscationStrategy(_random, _logger),
            new RenamingStrategy(_random, _logger)
            // Additional strategies would be added here
        ];
    }

    private async Task<ProtectionResult> PreprocessAsync(string inputPath, ProtectionConfiguration config)
    {
        var errors = new List<Exception>();

        try
        {
            // Validate input file
            if (!File.Exists(inputPath))
            {
                errors.Add(new FileNotFoundException($"Input file not found: {inputPath}"));
            }

            // Validate configuration
            var validationResult = ValidateConfiguration(config);
            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(e => new InvalidOperationException(e)));
            }

            // Log warnings
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning(warning);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return new ProtectionResult(
            Success: errors.Count == 0,
            AppliedStrategies: ImmutableArray<string>.Empty,
            Errors: errors.ToImmutableArray(),
            Duration: TimeSpan.Zero,
            Diagnostics: ImmutableArray<DiagnosticMessage>.Empty
        );
    }

    private ModuleDef? LoadModule(string inputPath)
    {
        try
        {
            _logger.LogInformation("Loading module: {Path}", inputPath);
            
            var module = ModuleDefMD.Load(inputPath);
            
            _logger.LogInformation("Module loaded successfully: {Name} v{Version}", 
                module.Name, module.Assembly?.Version ?? new Version(0, 0, 0, 0));
            
            return module;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load module");
            return null;
        }
    }

    private async Task<ProtectionResult> PostProcessAsync(
        ModuleDef module, 
        ProtectionConfiguration config, 
        ProtectionResult protectionResult)
    {
        var errors = new List<Exception>();

        try
        {
            _logger.LogInformation("Performing post-processing...");

            // Apply final optimizations
            if (config.Optimization >= OptimizationLevel.Minimal)
            {
                await ApplyFinalOptimizationsAsync(module, config);
            }

            // Validate result
            var validationResult = await ValidateProtectedModuleAsync(module, config);
            if (!validationResult.Success)
            {
                errors.AddRange(validationResult.Errors);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return new ProtectionResult(
            Success: errors.Count == 0,
            AppliedStrategies: protectionResult.AppliedStrategies,
            Errors: errors.ToImmutableArray(),
            Duration: protectionResult.Duration,
            Diagnostics: protectionResult.Diagnostics
        );
    }

    private async Task ApplyFinalOptimizationsAsync(ModuleDef module, ProtectionConfiguration config)
    {
        // Remove debug symbols if not preserving
        if (!config.PreserveDebugSymbols)
        {
            module.PdbState = null;
        }

        // Optimize IL code
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods.Where(m => m.HasBody))
            {
                method.Body.SimplifyMacros();
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

    private async Task<ProtectionResult> ValidateProtectedModuleAsync(ModuleDef module, ProtectionConfiguration config)
    {
        var errors = new List<Exception>();

        try
        {
            // Basic validation
            if (module.Types.Count == 0)
            {
                errors.Add(new InvalidOperationException("Protected module has no types"));
            }

            // Validate entry point if it exists
            if (module.EntryPoint != null)
            {
                if (!module.EntryPoint.HasBody)
                {
                    errors.Add(new InvalidOperationException("Entry point method has no body"));
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            errors.Add(ex);
        }

        return new ProtectionResult(
            Success: errors.Count == 0,
            AppliedStrategies: ImmutableArray<string>.Empty,
            Errors: errors.ToImmutableArray(),
            Duration: TimeSpan.Zero,
            Diagnostics: ImmutableArray<DiagnosticMessage>.Empty
        );
    }

    private async Task SaveModuleAsync(ModuleDef module, string outputPath)
    {
        try
        {
            _logger.LogInformation("Saving protected module to: {Path}", outputPath);

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Save module
            module.Write(outputPath);
            
            _logger.LogInformation("Module saved successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save module");
            throw;
        }
    }
}

/// <summary>
/// Result of configuration validation
/// </summary>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);