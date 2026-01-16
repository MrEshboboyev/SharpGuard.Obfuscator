using dnlib.DotNet;
using SharpGuard.Core.Configuration;

namespace SharpGuard.Core.Services;

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
/// Preserves metadata according to configuration
/// Implements Chain of Responsibility pattern
/// </summary>
public class MetadataPreserver(
    ProtectionConfiguration config
) : IMetadataPreserver
{
    public bool ShouldPreserve(TypeDef type)
    {
        // Preserve framework types
        if (type.FullName.StartsWith("System.") ||
            type.FullName.StartsWith("Microsoft."))
            return true;

        // Preserve explicitly excluded types
        if (config.ExcludedTypes.Contains(type.FullName))
            return true;

        // Preserve public API if configured
        if (config.PreservePublicApi && type.IsPublic)
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
        if (config.ExcludedMethods.Contains(method.FullName))
            return true;

        // Preserve public API if configured
        if (config.PreservePublicApi && (method.IsPublic || method.IsFamily))
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
        if (config.ExcludedMethods.Contains(field.FullName))
            return true;

        // Preserve public API if configured
        if (config.PreservePublicApi && field.IsPublic)
            return true;

        return false;
    }

    public void PreserveAttributes(IHasCustomAttribute member)
    {
        if (!config.PreserveCustomAttributes)
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
