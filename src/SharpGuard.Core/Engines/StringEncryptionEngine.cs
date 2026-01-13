using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Models;
using System.Text;

namespace SharpGuard.Core.Engines;

public class StringEncryptionEngine : ObfuscationEngineBase
{
    public override string Name => "String Encryption";
    public override string Description => "Matnlarni shifrlash va Runtime Decryptor metodini kiritish";

    protected override void Process(ObfuscationContext context)
    {
        MethodDef decryptMethod = InjectDecryptor(context.Module);

        foreach (var type in context.Module.GetTypes())
        {
            if (type.IsGlobalModuleType || type == decryptMethod.DeclaringType) continue;

            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;

                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode == OpCodes.Ldstr && instructions[i].Operand is string originalString)
                    {
                        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalString));

                        instructions[i].Operand = encoded;

                        instructions.Insert(i + 1, OpCodes.Call.ToInstruction(decryptMethod));

                        i++;
                    }
                }
                method.Body.OptimizeMacros();
            }
        }
    }

    private MethodDefUser InjectDecryptor(ModuleDef module)
    {
        var helperType = new TypeDefUser("SharpGuard", GetRandomName(), module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
        };
        module.Types.Add(helperType);

        var decryptMethod = new MethodDefUser(
            GetRandomName(),
            MethodSig.CreateStatic(module.CorLibTypes.String, module.CorLibTypes.String),
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);

        helperType.Methods.Add(decryptMethod);

        var body = new CilBody();
        decryptMethod.Body = body;

        var getUtf8 = typeof(Encoding).GetMethod("get_UTF8");
        var fromBase64 = typeof(Convert).GetMethod("FromBase64String", [typeof(string)]);
        var getString = typeof(Encoding).GetMethod("GetString", [typeof(byte[])]);

        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(getUtf8)));
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(fromBase64)));
        body.Instructions.Add(OpCodes.Callvirt.ToInstruction(module.Import(getString)));
        body.Instructions.Add(OpCodes.Ret.ToInstruction());

        return decryptMethod;
    }
}
