using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpGuard.Core.Services;

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
            )
            {
                Body = new CilBody()
            };

            type.Methods.Add(ctor);
        }
        return ctor;
    }
}
