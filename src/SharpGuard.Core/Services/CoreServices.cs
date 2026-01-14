using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Configuration;
using System.Security.Cryptography;

namespace SharpGuard.Core.Services;

/// <summary>
/// Simple logger abstraction
/// </summary>
public interface ILogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception? exception, string message, params object[] args);
    void LogDebug(string message, params object[] args);
}

/// <summary>
/// Random generator abstraction
/// </summary>
public interface IRandomGenerator
{
    int Next(int min, int max);
    byte[] NextBytes(int count);
    string NextString(int length);
}

/// <summary>
/// Metadata preservation service
/// </summary>
public interface IMetadataPreserver
{
    bool ShouldPreserve(TypeDef type);
    bool ShouldPreserve(MethodDef method);
    bool ShouldPreserve(FieldDef field);
    void PreserveAttributes(IHasCustomAttribute member);
}

/// <summary>
/// Cryptographically secure random generator
/// Implements Adapter pattern for RNG
/// </summary>
public class SecureRandomGenerator : IRandomGenerator
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public int Next(int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(min));
        if (min == max)
            return min;

        var range = (long)max - min;
        var bytes = new byte[4];
        _rng.GetBytes(bytes);
        
        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)((value % range) + min);
    }

    public byte[] NextBytes(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var bytes = new byte[count];
        _rng.GetBytes(bytes);
        return bytes;
    }

    public string NextString(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = NextBytes(length);
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }

    public double NextDouble()
    {
        var bytes = new byte[8];
        _rng.GetBytes(bytes);
        var value = BitConverter.ToUInt64(bytes, 0);
        return (double)value / ulong.MaxValue;
    }
}

/// <summary>
/// Console-based logger implementation
/// Implements Observer pattern
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly LogLevel _minimumLevel;

    public ConsoleLogger(LogLevel minimumLevel = LogLevel.Information)
    {
        _minimumLevel = minimumLevel;
    }

    public void LogInformation(string message, params object[] args)
    {
        if (_minimumLevel >= LogLevel.Information)
        {
            Console.WriteLine($"[INFO] {FormatMessage(message, args)}");
        }
    }

    public void LogWarning(string message, params object[] args)
    {
        if (_minimumLevel >= LogLevel.Warning)
        {
            Console.WriteLine($"[WARN] {FormatMessage(message, args)}");
        }
    }

    public void LogError(Exception? exception, string message, params object[] args)
    {
        if (_minimumLevel >= LogLevel.Error)
        {
            Console.WriteLine($"[ERROR] {FormatMessage(message, args)}");
            if (exception != null)
            {
                Console.WriteLine($"       {exception}");
            }
        }
    }

    public void LogDebug(string message, params object[] args)
    {
        if (_minimumLevel >= LogLevel.Debug)
        {
            Console.WriteLine($"[DEBUG] {FormatMessage(message, args)}");
        }
    }

    private static string FormatMessage(string message, object[] args)
    {
        try
        {
            return args.Length > 0 ? string.Format(message, args) : message;
        }
        catch
        {
            return message;
        }
    }
}

/// <summary>
/// Preserves metadata according to configuration
/// Implements Chain of Responsibility pattern
/// </summary>
public class MetadataPreserver : IMetadataPreserver
{
    private readonly ProtectionConfiguration _config;

    public MetadataPreserver(ProtectionConfiguration config)
    {
        _config = config;
    }

    public bool ShouldPreserve(TypeDef type)
    {
        // Preserve framework types
        if (type.FullName.StartsWith("System.") || 
            type.FullName.StartsWith("Microsoft."))
            return true;

        // Preserve explicitly excluded types
        if (_config.ExcludedTypes.Contains(type.FullName))
            return true;

        // Preserve public API if configured
        if (_config.PreservePublicApi && type.IsPublic)
            return true;

        return false;
    }

    public bool ShouldPreserve(MethodDef method)
    {
        // Preserve special methods
        if (method.IsConstructor || method.IsStaticConstructor || method.IsSpecialName)
            return true;

        // Preserve P/Invoke methods
        if (method.IsPinvokeImpl)
            return true;

        // Preserve interface implementations
        if (method.Overrides.Count > 0)
            return true;

        // Preserve explicitly excluded methods
        if (_config.ExcludedMethods.Contains(method.FullName))
            return true;

        // Preserve public API if configured
        if (_config.PreservePublicApi && (method.IsPublic || method.IsFamily))
            return true;

        return false;
    }

    public bool ShouldPreserve(FieldDef field)
    {
        // Preserve special fields
        if (field.IsSpecialName)
            return true;

        // Preserve literal fields
        if (field.IsLiteral)
            return true;

        // Preserve explicitly excluded fields
        if (_config.ExcludedMethods.Contains(field.FullName))
            return true;

        // Preserve public API if configured
        if (_config.PreservePublicApi && field.IsPublic)
            return true;

        return false;
    }

    public void PreserveAttributes(IHasCustomAttribute member)
    {
        if (!_config.PreserveCustomAttributes)
            return;

        // Remove obfuscation-incompatible attributes
        var attributesToRemove = new List<CustomAttribute>();
        
        foreach (var attr in member.CustomAttributes)
        {
            var attrType = attr.AttributeType.FullName;
            
            // Remove debugger attributes
            if (attrType.Contains("Debugger") || 
                attrType.Contains("DebuggerHidden") ||
                attrType.Contains("DebuggerStepThrough"))
            {
                attributesToRemove.Add(attr);
            }
        }

        // Remove incompatible attributes
        foreach (var attr in attributesToRemove)
        {
            member.CustomAttributes.Remove(attr);
        }
    }
}

/// <summary>
/// Extension methods for protection-related operations
/// </summary>
public static class ProtectionExtensions
{
    public static bool IsCompilerGenerated(this MethodDef method)
    {
        return method.CustomAttributes.Any(attr => 
            attr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
    }

    public static IEnumerable<TypeDef> GetTypes(this ModuleDef module)
    {
        return module.Types.Where(t => !t.IsGlobalModuleType);
    }

    public static MethodDef FindOrCreateStaticConstructor(this TypeDef type)
    {
        var ctor = type.Methods.FirstOrDefault(m => m.IsStaticConstructor);
        if (ctor == null)
        {
            ctor = new MethodDefUser(
                ".cctor",
                MethodSig.CreateStatic(type.Module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName
            );
            ctor.Body = new CilBody();
            type.Methods.Add(ctor);
        }
        return ctor;
    }
}