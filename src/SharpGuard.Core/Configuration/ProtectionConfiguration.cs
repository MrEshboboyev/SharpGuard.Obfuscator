using System.Collections.Immutable;

namespace SharpGuard.Core.Configuration;

/// <summary>
/// Main configuration for the protection system
/// Implements Builder pattern for fluent configuration
/// </summary>
public class ProtectionConfiguration
{
    public bool EnableControlFlowObfuscation { get; set; } = true;
    public bool EnableStringEncryption { get; set; } = true;
    public bool EnableAntiDebugging { get; set; } = true;
    public bool EnableAntiTampering { get; set; } = true;
    public bool EnableRenaming { get; set; } = true;
    public bool EnableWatermarking { get; set; } = false;
    public bool EnableVirtualization { get; set; } = false;
    public bool EnableMutation { get; set; } = false;
    public bool EnableConstantsEncoding { get; set; } = true;
    public bool EnableResourcesProtection { get; set; } = true;
    public bool EnableCallIndirection { get; set; } = true;
    public bool EnableJunkCodeInsertion { get; set; } = false;
    
    public RenamingOptions Renaming { get; set; } = new();
    public ControlFlowOptions ControlFlow { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
    public AntiTamperOptions AntiTamper { get; set; } = new();
    public VirtualizationOptions Virtualization { get; set; } = new();
    public MutationOptions Mutation { get; set; } = new();
    public JunkCodeOptions JunkCode { get; set; } = new();
    
    public ImmutableHashSet<string> ExcludedNamespaces { get; set; } = [];
    public ImmutableHashSet<string> ExcludedTypes { get; set; } = [];
    public ImmutableHashSet<string> ExcludedMethods { get; set; } = [];
    
    public OptimizationLevel Optimization { get; set; } = OptimizationLevel.Balanced;
    public DebugMode Debug { get; set; } = DebugMode.None;
    
    public bool PreserveDebugSymbols { get; set; } = false;
    public bool PreserveCustomAttributes { get; set; } = true;
    public bool PreservePublicApi { get; set; } = true;
    
    public string OutputPath { get; set; } = string.Empty;
    public string KeyContainer { get; set; } = string.Empty;
    public byte[]? StrongNameKey { get; set; }
    
    public int ParallelismLevel { get; set; } = Environment.ProcessorCount;
    public bool EnableLogging { get; set; } = true;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    
    public Dictionary<string, object> CustomSettings { get; set; } = [];

    /// <summary>
    /// Creates a builder for fluent configuration
    /// </summary>
    public static ProtectionConfigurationBuilder CreateBuilder() => new();
}

/// <summary>
/// Fluent builder for ProtectionConfiguration
/// </summary>
public class ProtectionConfigurationBuilder
{
    private readonly ProtectionConfiguration _config = new();

    public ProtectionConfigurationBuilder WithControlFlow(bool enabled = true)
    {
        _config.EnableControlFlowObfuscation = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithStringEncryption(bool enabled = true)
    {
        _config.EnableStringEncryption = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithAntiDebugging(bool enabled = true)
    {
        _config.EnableAntiDebugging = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithAntiTampering(bool enabled = true)
    {
        _config.EnableAntiTampering = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithRenaming(bool enabled = true)
    {
        _config.EnableRenaming = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithVirtualization(bool enabled = true)
    {
        _config.EnableVirtualization = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder WithMutation(bool enabled = true)
    {
        _config.EnableMutation = enabled;
        return this;
    }

    public ProtectionConfigurationBuilder ExcludeNamespace(params string[] namespaces)
    {
        _config.ExcludedNamespaces = _config.ExcludedNamespaces.Union(namespaces);
        return this;
    }

    public ProtectionConfigurationBuilder ExcludeType(params string[] types)
    {
        _config.ExcludedTypes = _config.ExcludedTypes.Union(types).ToImmutableHashSet();
        return this;
    }

    public ProtectionConfigurationBuilder Optimize(OptimizationLevel level)
    {
        _config.Optimization = level;
        return this;
    }

    public ProtectionConfigurationBuilder SetOutputPath(string path)
    {
        _config.OutputPath = path;
        return this;
    }

    public ProtectionConfiguration Build() => _config;
}

public enum OptimizationLevel
{
    None,
    Minimal,
    Balanced,
    Aggressive
}

public enum DebugMode
{
    None,
    SymbolsOnly,
    Full
}

public enum LogLevel
{
    None,
    Critical,
    Error,
    Warning,
    Information,
    Debug,
    Trace
}

public class RenamingOptions
{
    public bool Enabled { get; set; } = true;
    public RenamingMode Mode { get; set; } = RenamingMode.Aggressive;
    public bool RenamePublicMembers { get; set; } = false;
    public bool RenameEnumMembers { get; set; } = true;
    public bool RenameProperties { get; set; } = true;
    public bool RenameEvents { get; set; } = true;
    public bool RenameFields { get; set; } = true;
    public bool FlattenNamespaces { get; set; } = false;
    public string NamespacePrefix { get; set; } = "_";
    public bool GenerateMappingFile { get; set; } = false;
}

public enum RenamingMode
{
    None,
    Light,
    Normal,
    Aggressive
}

public class ControlFlowOptions
{
    public bool Enabled { get; set; } = true;
    public ControlFlowMode Mode { get; set; } = ControlFlowMode.Normal;
    public int ComplexityThreshold { get; set; } = 10;
    public bool InsertJunkBlocks { get; set; } = true;
    public bool SplitMethods { get; set; } = false;
}

public enum ControlFlowMode
{
    None,
    Light,
    Normal,
    Heavy,
    Extreme
}

public class EncryptionOptions
{
    public bool Enabled { get; set; } = true;
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES;
    public bool EncryptMethods { get; set; } = true;
    public bool EncryptStrings { get; set; } = true;
    public bool EncryptResources { get; set; } = true;
    public bool DynamicDecryption { get; set; } = true;
}

public enum EncryptionAlgorithm
{
    AES,
    RSA,
    ChaCha20,
    Custom
}

public class AntiTamperOptions
{
    public bool Enabled { get; set; } = true;
    public AntiTamperMode Mode { get; set; } = AntiTamperMode.Normal;
    public bool ValidateChecksum { get; set; } = true;
    public bool ValidateSignature { get; set; } = true;
    public bool CorruptOnTamper { get; set; } = false;
}

public enum AntiTamperMode
{
    None,
    Light,
    Normal,
    Heavy
}

public class VirtualizationOptions
{
    public bool Enabled { get; set; } = false;
    public double VirtualizationPercentage { get; set; } = 0.3;
    public bool VirtualizeMethods { get; set; } = true;
    public bool VirtualizeTypes { get; set; } = false;
    public VirtualizationEngine Engine { get; set; } = VirtualizationEngine.Custom;
}

public enum VirtualizationEngine
{
    Custom,
    Eazfuscator,
    SmartAssembly
}

public class MutationOptions
{
    public bool Enabled { get; set; } = false;
    public MutationStrength Strength { get; set; } = MutationStrength.Medium;
    public bool MutateArithmetic { get; set; } = true;
    public bool MutateLogic { get; set; } = true;
    public bool MutateControlFlow { get; set; } = true;
}

public enum MutationStrength
{
    Low,
    Medium,
    High,
    Extreme
}

public class JunkCodeOptions
{
    public bool Enabled { get; set; } = false;
    public JunkCodeDensity Density { get; set; } = JunkCodeDensity.Low;
    public bool InsertInMethods { get; set; } = true;
    public bool InsertInTypes { get; set; } = false;
    public bool ObfuscateJunkCode { get; set; } = true;
}

public enum JunkCodeDensity
{
    None,
    Low,
    Medium,
    High,
    Extreme
}
