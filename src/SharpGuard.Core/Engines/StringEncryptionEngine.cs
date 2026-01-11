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
        // 1. Dekoder metodini DLL ichiga joylashtiramiz
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
                        // Matnni shifrlaymiz
                        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalString));

                        // Asl matnni shifrlangani bilan almashtiramiz
                        instructions[i].Operand = encoded;

                        // Shifrlangan matndan so'ng dekoder metodini chaqirish buyrug'ini qo'shamiz
                        // ldstr "SGVsbG8..." -> call Decryptor
                        instructions.Insert(i + 1, OpCodes.Call.ToInstruction(decryptMethod));

                        i++; // Yangi qo'shilgan instruction ustidan sakrab o'tamiz
                    }
                }
                method.Body.OptimizeMacros(); // Kodni optimizatsiya qilish
            }
        }
    }

    // DLL ichiga "Decryptor" metodini inject qilish
    private MethodDefUser InjectDecryptor(ModuleDef module)
    {
        // Yangi yashirin klass yaratamiz
        var helperType = new TypeDefUser("SharpGuard", GetRandomName(), module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
        };
        module.Types.Add(helperType);

        // Decrypt metodini yaratamiz: public static string Decrypt(string input)
        var decryptMethod = new MethodDefUser(
            GetRandomName(),
            MethodSig.CreateStatic(module.CorLibTypes.String, module.CorLibTypes.String),
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);

        helperType.Methods.Add(decryptMethod);

        // Metod tanasi (CIL kodida Base64 dekodlash)
        var body = new CilBody();
        decryptMethod.Body = body;

        // System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(input)) mantiqi:
        var getUtf8 = typeof(Encoding).GetMethod("get_UTF8");
        var fromBase64 = typeof(Convert).GetMethod("FromBase64String", [typeof(string)]);
        var getString = typeof(Encoding).GetMethod("GetString", [typeof(byte[])]);

        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(getUtf8)));
        // Parametrdagi stringni yuklash (Argument 0)
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(fromBase64)));
        body.Instructions.Add(OpCodes.Callvirt.ToInstruction(module.Import(getString)));
        // Metoddan qiymatni qaytarish (Return)
        body.Instructions.Add(OpCodes.Ret.ToInstruction());

        return decryptMethod;
    }
}
