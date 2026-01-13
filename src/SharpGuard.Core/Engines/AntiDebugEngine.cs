using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Models;
using System.Diagnostics;

namespace SharpGuard.Core.Engines;

public class AntiDebugEngine : ObfuscationEngineBase
{
    public override string Name => "Anti-Debug";
    public override string Description => "Win32 API va Managed debugger tekshiruvlarini inject qilish";

    protected override void Process(ObfuscationContext context)
    {
        var module = context.Module;

        var helperType = new TypeDefUser(
            @namespace: "SharpGuard",
            name: GetRandomName(),
            baseType: module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
        };

        module.Types.Add(helperType);
        _ = module.CorLibTypes.GetTypeRef("System.Runtime.InteropServices", "DllImportAttribute");

        var checkMethod = new MethodDefUser(
            GetRandomName(),
            MethodSig.CreateStatic(module.CorLibTypes.Void),
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);

        helperType.Methods.Add(checkMethod);

        var body = new CilBody();
        checkMethod.Body = body;

        var isAttachedGetter = typeof(Debugger).GetMethod("get_IsAttached");
        var exitMethod = typeof(Environment).GetMethod("Exit", [typeof(int)]);

        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(isAttachedGetter)));
        var label = OpCodes.Nop.ToInstruction();
        body.Instructions.Add(OpCodes.Brfalse_S.ToInstruction(label));

        body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(exitMethod)));

        body.Instructions.Add(label);
        body.Instructions.Add(OpCodes.Ret.ToInstruction());

        var entryPoint = module.EntryPoint;
        if (entryPoint != null && entryPoint.HasBody)
        {
            entryPoint.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(checkMethod));
        }
    }
}
