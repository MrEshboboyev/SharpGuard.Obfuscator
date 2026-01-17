using SharpGuard.CLI;
using SharpGuard.Core;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Orchestration;
using SharpGuard.Core.Services;

Console.WriteLine("=== SharpGuard Advanced Obfuscator v2.0 ===");

var parsedArgs = Arguments.Parse(args);
if (parsedArgs == null || string.IsNullOrEmpty(parsedArgs.InputPath))
{
    Console.WriteLine("Usage: SharpGuard.CLI <input_path> [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  --output <path>     Output file path");
    Console.WriteLine("  --config <path>     Configuration file path");
    Console.WriteLine("  --level <level>     Protection level (None|Minimal|Balanced|Aggressive)");
    Console.WriteLine("  --no-renaming       Disable renaming");
    Console.WriteLine("  --no-stringenc      Disable string encryption");
    Console.WriteLine("  --no-controlflow    Disable control flow obfuscation");
    Console.WriteLine("  --no-antidebug      Disable anti-debugging");
    return;
}

try
{
    // Load configuration
    var config = LoadConfiguration(parsedArgs);
    
    // Validate configuration
    var validationResult = AdvancedProtector.ValidateConfiguration(config);
    if (!validationResult.IsValid)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Configuration errors:");
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"  - {error}");
        }
        Console.ResetColor();
        return;
    }
    
    // Display warnings
    if (validationResult.Warnings.Any())
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Configuration warnings:");
        foreach (var warning in validationResult.Warnings)
        {
            Console.WriteLine($"  - {warning}");
        }
        Console.ResetColor();
    }
    
    // Create protector
    var logger = new ConsoleLogger(config.MinimumLogLevel);
    var protector = new AdvancedProtector(logger);
    
    // Execute protection
    var result = await protector.ProtectAsync(parsedArgs.InputPath, config);
    
    // Display results
    DisplayResults(result, logger);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[!] Fatal error occurred: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.ResetColor();
    Environment.ExitCode = 1;
}

static ProtectionConfiguration LoadConfiguration(Arguments args)
{
    var builder = ProtectionConfiguration.CreateBuilder();
    
    // Set output path
    if (!string.IsNullOrEmpty(args.OutputPath))
    {
        builder.SetOutputPath(args.OutputPath);
    }
    else
    {
        var inputDir = Path.GetDirectoryName(args.InputPath);
        var inputName = Path.GetFileNameWithoutExtension(args.InputPath);
        var inputExt = Path.GetExtension(args.InputPath);
        builder.SetOutputPath(Path.Combine(inputDir ?? ".", $"{inputName}_protected{inputExt}"));
    }
    
    // Apply command-line options
    if (args.DisableRenaming)
        builder.WithRenaming(false);
        
    if (args.DisableStringEncryption)
        builder.WithStringEncryption(false);
        
    if (args.DisableControlFlow)
        builder.WithControlFlow(false);
        
    if (args.DisableAntiDebugging)
        builder.WithAntiDebugging(false);
    
    // Set optimization level
    if (!string.IsNullOrEmpty(args.Level))
    {
        var level = Enum.TryParse<OptimizationLevel>(args.Level, true, out var parsedLevel) 
            ? parsedLevel 
            : OptimizationLevel.Balanced;
        builder.Optimize(level);
    }
    
    return builder.Build();
}

static void DisplayResults(ProtectionResult result, ILogger logger)
{
    if (result.Success)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[✔] Protection completed successfully!");
        Console.ResetColor();
        
        Console.WriteLine($"Applied strategies ({result.AppliedStrategies.Length}): ");
        foreach (var strategy in result.AppliedStrategies)
        {
            Console.WriteLine($"  • {strategy}");
        }
        
        Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");
        
        if (result.Diagnostics.Any())
        {
            Console.WriteLine("\nDiagnostics:");
            var infoCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);
            var warningCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
            var errorCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
            
            if (infoCount > 0) Console.WriteLine($"  Info: {infoCount}");
            if (warningCount > 0) 
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warnings: {warningCount}");
                Console.ResetColor();
            }
            if (errorCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Errors: {errorCount}");
                Console.ResetColor();
            }
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[✗] Protection failed!");
        Console.ResetColor();
        
        Console.WriteLine("Errors:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"  • {error.Message}");
        }
        
        Environment.ExitCode = 1;
    }
}
