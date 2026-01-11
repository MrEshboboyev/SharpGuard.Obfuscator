using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public class Watermarking : ObfuscationEngineBase
{
    public override string Name => "Watermarking";
    public override string Description => "Assemblyga maxfiy mualliflik belgisini qo'shish";

    protected override void Process(ObfuscationContext context)
    {
        var attrType = new TypeDefUser(
            "SharpGuard",
            "SharpGuardAttribute",
            context.Module.CorLibTypes.GetTypeRef("System", "Attribute")
        );

        var stringType = context.Module.CorLibTypes.String;

        var ctor = new MethodDefUser(
            ".ctor",
            MethodSig.CreateInstance(context.Module.CorLibTypes.Void, stringType),
            MethodAttributes.HideBySig | MethodAttributes.Public |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName
        );

        // Body qo'shish
        var body = new CilBody();
        body.Instructions.Add(OpCodes.Ret.ToInstruction());
        ctor.Body = body;

        attrType.Methods.Add(ctor);
        context.Module.Types.Add(attrType);

        string watermarkMessage = $"Protected by SharpGuard v1.0 - {DateTime.Now:yyyy}";
        var customAttribute = new CustomAttribute(ctor);
        customAttribute.ConstructorArguments.Add(new CAArgument(stringType, watermarkMessage));

        context.Module.CustomAttributes.Add(customAttribute);
    }
}
