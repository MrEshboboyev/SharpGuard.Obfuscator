using SharpGuard.Core.Engines;
using SharpGuard.Core.Models;

namespace SharpGuard.Core;

public class Protector(string inputPath, string outputPath)
{
    private readonly ObfuscationContext _context = new(inputPath, outputPath);
    private readonly List<IObfuscationEngine> _engines =
    [
        new RenamingEngine(),
        new StringEncryptionEngine(),
        new ControlFlowEngine(),
        new Watermarking()
    ];

    public void Execute()
    {
        foreach (var engine in _engines)
        {
            engine.Execute(_context);
        }

        _context.Save();
    }
}
