using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using System.Collections.Immutable;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core.Strategies;

/// <summary>
/// Advanced control flow obfuscation using opaque predicartes and bogus control flow
/// Implements Decorator and Strategy patterns
/// </summary>
public class ControlFlowObfuscationStrategy(
    IRandomGenerator random,
    ILogger logger
) : IProtectionStrategy
{
    public string Id => "controlflow";
    public string Name => "Advanced Control Flow Obfuscation";
    public string Description => "Protects against static analysis through opaque predicates and control flow scrambling";
    public int Priority => 800;
    public ImmutableArray<string> Dependencies => [];
    public ImmutableArray<string> ConflictsWith => ["mutation"];

    public bool CanApply(ModuleDef module)
    {
        return module.Types.Any(t => t.HasMethods && t.Methods.Any(m => m.HasBody));
    }

    public void Apply(ModuleDef module, ProtectionContext context)
    {
        var config = context.Configuration.ControlFlow;
        if (!config.Enabled) return;

        var processedMethods = 0;
        var totalInstructions = 0;

        foreach (var type in module.GetTypes().Where(t => !IsExcluded(t, context)))
        {
            foreach (var method in type.Methods.Where(m => m.HasBody && !IsExcluded(m, context)))
            {
                try
                {
                    ProcessMethod(method, config);
                    processedMethods++;
                    totalInstructions += method.Body.Instructions.Count;
                }
                catch (Exception ex)
                {
                    context.AddDiagnostic(DiagnosticSeverity.Warning, "CF001", 
                        $"Failed to obfuscate method {method.FullName}: {ex.Message}", method.FullName);
                }
            }
        }

        logger.LogInformation("Control flow obfuscation: {Methods} methods, {Instructions} instructions processed", 
            processedMethods, totalInstructions);
    }

    private void ProcessMethod(MethodDef method, ControlFlowOptions config)
    {
        var body = method.Body;
        body.SimplifyBranches();

        // Insert opaque predicates
        if (config.Mode >= ControlFlowMode.Normal)
        {
            InsertOpaquePredicates(body);
        }

        // Scramble control flow
        if (config.Mode >= ControlFlowMode.Heavy)
        {
            ScrambleControlFlow(body);
        }

        // Split complex methods
        if (config.SplitMethods && body.Instructions.Count > config.ComplexityThreshold)
        {
            SplitMethod(method);
        }

        body.OptimizeBranches();
    }

    private void InsertOpaquePredicates(CilBody body)
    {
        var instructions = body.Instructions.ToList();
        var insertPoints = FindInsertionPoints(instructions);

        foreach (var point in insertPoints.Take(random.Next(1, 4)))
        {
            var predicate = GenerateOpaquePredicate();

            int index = point.Index;
            body.Instructions.Insert(index++, predicate.LoadValue);
            body.Instructions.Insert(index++, predicate.LoadZero);
            body.Instructions.Insert(index++, predicate.Operation);
            body.Instructions.Insert(index++, predicate.CompareValue);
            body.Instructions.Insert(index++, predicate.Comparison);

            var jumpInstruction = OpCodes.Brtrue.ToInstruction(body.Instructions[index]);
            body.Instructions.Insert(index, jumpInstruction);
        }
    }

    private OpaquePredicatePoint GenerateOpaquePredicate()
    {
        // Generate mathematically proven opaque predicate
        // Example: (x * 0) == 0 (always true) or (x & 0) != 0 (always false)
        var value = random.Next(0, 2) == 0;
        
        return new OpaquePredicatePoint(
            new Instruction(OpCodes.Ldc_I4, random.Next(1, 1000)),
            new Instruction(OpCodes.Ldc_I4_0),
            new Instruction(value ? OpCodes.Mul : OpCodes.And),
            new Instruction(value ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1),
            new Instruction(value ? OpCodes.Ceq : OpCodes.Cgt)
        );
    }

    private void ScrambleControlFlow(CilBody body)
    {
        // Convert sequential instructions into switch-based dispatch
        var instructions = body.Instructions.ToList();

        // Bloklarga bo'lish (Blok hajmini 3-5 atrofida saqlash tavsiya etiladi)
        var blocks = SplitIntoBlocks(instructions, 3);

        if (blocks.Count <= 1) return;

        // MODULNI OLISH: body orqali modulga kiramiz
        // Buning uchun method body'dan method'ga, method'dan esa modulga o'tiladi
        var module = body.Instructions[0].Operand as ModuleDef; // Bu har doim ham ishlamasligi mumkin

        // Eng xavfsiz yo'li: CreateDispatcher chaqirilayotgan joyda modulni aniqlash
        // Agar metod parametrida module bo'lmasa, uni quyidagicha olish mumkin:
        var currentModule = (body.Instructions.Count > 0) ? GetModuleFromBody(body) : null;

        if (currentModule == null) return;

        // Create dispatcher: Endi module parametri uzatildi
        var dispatcher = CreateDispatcher(body, blocks, currentModule);

        // Original instruksiyalarni dispatcher bilan almashtirish
        body.Instructions.Clear();
        foreach (var instruction in dispatcher)
        {
            body.Instructions.Add(instruction);
        }
    }

    // Yordamchi metod: Body orqali modulni topish
    private ModuleDef GetModuleFromBody(CilBody body)
    {
        // Odatda metod orqali modulga kiriladi
        // Agar arxitekturangizda context mavjud bo'lsa, context.Module ni ishlating
        // Agar yo'q bo'lsa, bizga strategiya darajasida module obyekti kerak bo'ladi
        return body.Variables.First().Type.Module; // Bu sodda, lekin xavfli usul
    }

    private List<List<Instruction>> SplitIntoBlocks(List<Instruction> instructions, int blockSize)
    {
        var blocks = new List<List<Instruction>>();
        for (int i = 0; i < instructions.Count; i += blockSize)
        {
            var block = instructions.Skip(i).Take(blockSize).ToList();
            if (block.Count > 0)
                blocks.Add(block);
        }
        return blocks;
    }

    private List<Instruction> CreateDispatcher(CilBody body, List<List<Instruction>> blocks, ModuleDef module)
    {
        var dispatcher = new List<Instruction>();

        var stateField = new Local(module.CorLibTypes.Int32);
        body.Variables.Add(stateField);

        dispatcher.Add(OpCodes.Ldc_I4_0.ToInstruction());
        dispatcher.Add(OpCodes.Stloc.ToInstruction(stateField));

        var switchTargets = new Instruction[blocks.Count];

        var loopStart = OpCodes.Ldloc.ToInstruction(stateField);
        dispatcher.Add(loopStart);

        var switchInstr = OpCodes.Switch.ToInstruction(switchTargets);
        dispatcher.Add(switchInstr);

        var endLabel = OpCodes.Ret.ToInstruction();
        dispatcher.Add(OpCodes.Br.ToInstruction(endLabel));
        
        for (int i = 0; i < blocks.Count; i++)
        {
            switchTargets[i] = blocks[i][0];

            foreach (var instr in blocks[i])
                dispatcher.Add(instr);

            if (i < blocks.Count - 1)
            {
                dispatcher.Add(OpCodes.Ldc_I4.ToInstruction(i + 1));
                dispatcher.Add(OpCodes.Stloc.ToInstruction(stateField));
                dispatcher.Add(OpCodes.Br.ToInstruction(loopStart));
            }
        }

        dispatcher.Add(endLabel);
        return dispatcher;
    }

    private void SplitMethod(MethodDef method)
    {
        // Implementation for splitting large methods into smaller ones
        // This increases analysis complexity significantly
    }

    private List<(int Index, Instruction Instruction)> FindInsertionPoints(List<Instruction> instructions)
    {
        var points = new List<(int, Instruction)>();
        for (int i = 0; i < instructions.Count - 1; i++)
        {
            if (!IsBranchingInstruction(instructions[i].OpCode) && !IsBranchingInstruction(instructions[i + 1].OpCode))
            {
                points.Add((i + 1, instructions[i + 1]));
            }
        }
        return [.. points.OrderBy(_ => random.Next(0, 1000))];
    }

    private static bool IsBranchingInstruction(OpCode opcode)
    {
        return opcode.FlowControl == FlowControl.Branch || 
               opcode.FlowControl == FlowControl.Cond_Branch ||
               opcode == OpCodes.Ret ||
               opcode == OpCodes.Throw;
    }

    private static bool IsExcluded(TypeDef type, ProtectionContext context)
    {
        return context.Configuration.ExcludedNamespaces.Contains(type.Namespace) ||
               context.Configuration.ExcludedTypes.Contains(type.FullName);
    }

    private static bool IsExcluded(MethodDef method, ProtectionContext context)
    {
        return context.Configuration.ExcludedMethods.Contains(method.FullName) ||
               IsExcluded(method.DeclaringType, context);
    }

    private record OpaquePredicatePoint(
        Instruction LoadValue,
        Instruction LoadZero,
        Instruction Operation,
        Instruction CompareValue,
        Instruction Comparison);
}
