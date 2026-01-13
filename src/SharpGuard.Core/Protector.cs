using SharpGuard.Core.Engines;
using SharpGuard.Core.Helpers;
using SharpGuard.Core.Models;

namespace SharpGuard.Core;

public class Protector(string inputPath, string outputPath)
{
    private readonly ObfuscationContext _context = new(inputPath, outputPath);
    private readonly List<IObfuscationEngine> _engines =
        [
            new Watermarking(),
            new AntiDebugEngine(),
            new StringEncryptionEngine(),
            new ControlFlowEngine(),
            new RenamingEngine()
        ];

    public void Execute()
    {
        foreach (var engine in _engines)
        {
            engine.Execute(_context);
        }

        FinalizeModule();

        _context.Save();
    }

    private void FinalizeModule()
    {
        foreach (var type in _context.Module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                method.SimplifyAndOptimize();
            }
        }
    }
}
