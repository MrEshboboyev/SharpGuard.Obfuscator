using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpGuard.Core.Helpers;

public static class DnlibExtensions
{
    public static bool CanObfuscate(this MethodDef method)
    {
        if (!method.HasBody || !method.Body.HasInstructions) return false;
        if (method.IsRuntimeSpecialName || method.IsSpecialName) return false;
        if (method.DeclaringType.IsGlobalModuleType) return false;

        return true;
    }

    public static void InsertJunk(this IList<Instruction> instructions, int index)
    {
        instructions.Insert(index, OpCodes.Ldc_I4_0.ToInstruction());
        instructions.Insert(index + 1, OpCodes.Brfalse.ToInstruction(instructions[index + 2]));
    }

    public static void SimplifyAndOptimize(this MethodDef method)
    {
        method.Body.SimplifyMacros(method.Parameters);
        method.Body.OptimizeMacros();
    }
}
