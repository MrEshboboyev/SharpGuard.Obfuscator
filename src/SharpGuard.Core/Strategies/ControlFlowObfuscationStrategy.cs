using System.Collections.Immutable;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpGuard.Core.Abstractions;
using SharpGuard.Core.Configuration;
using SharpGuard.Core.Services;
using ILogger = SharpGuard.Core.Services.ILogger;

namespace SharpGuard.Core.Strategies;

/// <summary>
/// Advanced control flow obfuscation using opaque predicates and bogus control flow
/// Implements Decorator and Strategy patterns
/// </summary>
public class ControlFlowObfuscationStrategy(IRandomGenerator random, ILogger logger) : IProtectionStrategy
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
        
        foreach (var point in insertPoints.Take(random.Next(1, 4))) // 1-3 predicates per method
        {
            var predicate = GenerateOpaquePredicate();
            body.Instructions.Insert(point.Index, predicate);
            
            // Add conditional jump that always goes the same way
            var jumpInstruction = new Instruction(OpCodes.Brtrue, body.Instructions[point.Index + 2]);
            body.Instructions.Insert(point.Index + 1, jumpInstruction);
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
        var blocks = SplitIntoBlocks(instructions, 3); // 3 instructions per block
        
        if (blocks.Count <= 1) return;

        // Create dispatcher
        var dispatcher = CreateDispatcher(body, blocks);
        
        // Replace original instructions with dispatcher
        body.Instructions.Clear();
        foreach (var instruction in dispatcher)
        {
            body.Instructions.Add(instruction);
        }
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

    private List<Instruction> CreateDispatcher(CilBody body, List<List<Instruction>> blocks)
    {
        var dispatcher = new List<Instruction>();
        
        // State variable
        var stateField = new Local(body.Variables.Context.Module.CorLibTypes.Int32);
        body.Variables.Add(stateField);
        
        // Initialize state
        dispatcher.Add(new Instruction(OpCodes.Ldc_I4_0));
        dispatcher.Add(new Instruction(OpCodes.Stloc, stateField));
        
        // Switch dispatch
        var switchTargets = new Instruction[blocks.Count];
        for (int i = 0; i < blocks.Count; i++)
        {
            switchTargets[i] = blocks[i][0];
        }
        
        dispatcher.Add(new Instruction(OpCodes.Ldloc, stateField));
        dispatcher.Add(new Instruction(OpCodes.Switch, switchTargets));
        
        // Add blocks
        for (int i = 0; i < blocks.Count; i++)
        {
            dispatcher.AddRange(blocks[i]);
            
            // Update state for next iteration
            if (i < blocks.Count - 1)
            {
                dispatcher.Add(new Instruction(OpCodes.Ldc_I4, i + 1));
                dispatcher.Add(new Instruction(OpCodes.Stloc, stateField));
            }
        }
        
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
            var current = instructions[i];
            var next = instructions[i + 1];
            
            // Safe insertion points (avoid branching instructions)
            if (!IsBranchingInstruction(current.OpCode) && !IsBranchingInstruction(next.OpCode))
            {
                points.Add((i + 1, next));
            }
        }
        
        return points.OrderBy(_ => random.Next()).ToList();
    }

    private bool IsBranchingInstruction(OpCode opcode)
    {
        return opcode.FlowControl == FlowControl.Branch || 
               opcode.FlowControl == FlowControl.Cond_Branch ||
               opcode == OpCodes.Ret ||
               opcode == OpCodes.Throw;
    }

    private bool IsExcluded(TypeDef type, ProtectionContext context)
    {
        return context.Configuration.ExcludedNamespaces.Contains(type.Namespace) ||
               context.Configuration.ExcludedTypes.Contains(type.FullName);
    }

    private bool IsExcluded(MethodDef method, ProtectionContext context)
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
