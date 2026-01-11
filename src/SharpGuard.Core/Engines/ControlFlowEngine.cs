using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public class ControlFlowEngine : ObfuscationEngineBase
{
    public override string Name => "Control Flow";
    public override string Description => "Kod oqimini Switch-based State Machine orqali chalkashtirish";

    protected override void Process(ObfuscationContext context)
    {
        foreach (var type in context.Module.GetTypes())
        {
            if (type.IsGlobalModuleType) continue;
            foreach (var method in type.Methods)
            {
                if (!method.HasBody || !method.Body.HasInstructions || method.Body.Instructions.Count < 3) continue;

                // Metod oqimini chalkashtirish
                ExecuteControlFlowFlattening(method);
            }
        }
    }

    private static void ExecuteControlFlowFlattening(MethodDef method)
    {
        var body = method.Body;
        body.SimplifyMacros(method.Parameters);

        var instructions = body.Instructions;
        var blocks = BaseBlock.Split(method);

        // Bloklarni aralashtirish (Shuffling)
        if (blocks.Count < 2) return;
        var shuffledBlocks = blocks.OrderBy(x => Guid.NewGuid()).ToList();

        // Yangi body tayyorlash
        body.Instructions.Clear();
        var stateVar = new Local(method.Module.CorLibTypes.Int32);
        body.Variables.Add(stateVar);

        var switchHeader = OpCodes.Nop.ToInstruction();
        var defaultLabel = OpCodes.Ret.ToInstruction();

        // 1. Kirish nuqtasi: State = birinchi blokning ID si
        body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(blocks[0].Id));
        body.Instructions.Add(OpCodes.Stloc.ToInstruction(stateVar));
        body.Instructions.Add(OpCodes.Br.ToInstruction(switchHeader));

        // 2. Switch mexanizmi
        body.Instructions.Add(switchHeader);
        body.Instructions.Add(OpCodes.Ldloc.ToInstruction(stateVar));

        var switchInstr = OpCodes.Switch.ToInstruction(new Instruction[blocks.Count]);
        body.Instructions.Add(switchInstr);
        body.Instructions.Add(OpCodes.Br.ToInstruction(defaultLabel));

        // 3. Har bir blokni joylashtirish
        var targetLabels = new Instruction[blocks.Count];
        foreach (var block in shuffledBlocks)
        {
            var blockStart = block.Instructions[0];
            targetLabels[block.Id] = blockStart;

            foreach (var instr in block.Instructions)
                body.Instructions.Add(instr);

            // Blok oxirida keyingi holatni o'rnatish
            int nextId = (block.Id + 1 < blocks.Count) ? blocks[block.Id + 1].Id : -1;
            if (nextId != -1)
            {
                body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(nextId));
                body.Instructions.Add(OpCodes.Stloc.ToInstruction(stateVar));
                body.Instructions.Add(OpCodes.Br.ToInstruction(switchHeader));
            }
            else
            {
                body.Instructions.Add(OpCodes.Br.ToInstruction(defaultLabel));
            }
        }

        switchInstr.Operand = targetLabels;
        body.Instructions.Add(defaultLabel);

        // Xatolik tuzatildi: metod parametrlarini uzatamiz
        body.OptimizeMacros();
    }

    // Ichki klass: Kodni mantiqiy bo'laklarga ajratish uchun
    private class BaseBlock
    {
        public int Id { get; set; }
        public List<Instruction> Instructions { get; set; } = [];

        public static List<BaseBlock> Split(MethodDef method)
        {
            var blocks = new List<BaseBlock>();
            var currentBlock = new BaseBlock { Id = 0 };
            blocks.Add(currentBlock);

            foreach (var instr in method.Body.Instructions)
            {
                currentBlock.Instructions.Add(instr);

                // Agar buyruq oqimni o'zgartirsa (branch, return) yangi blok boshlaymiz
                if (IsTerminator(instr.OpCode))
                {
                    currentBlock = new BaseBlock { Id = blocks.Count };
                    blocks.Add(currentBlock);
                }
            }
            return [.. blocks.Where(b => b.Instructions.Count > 0)];
        }

        private static bool IsTerminator(OpCode op)
        {
            return op.FlowControl switch
            {
                FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw => true,
                _ => false,
            };
        }
    }
}
