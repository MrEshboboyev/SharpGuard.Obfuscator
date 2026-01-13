using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public abstract class ObfuscationEngineBase : IObfuscationEngine
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    public void Execute(ObfuscationContext context)
    {
        Console.WriteLine($"[Engine] {Name} boshlandi: {Description}");
        
        // inherited engines execution
        Process(context);
        
        Console.WriteLine($"[Engine] {Name} yakunlandi.");
    }

    protected abstract void Process(ObfuscationContext context);

    protected string GetRandomName() => string.Concat("_", Guid.NewGuid().ToString("N").AsSpan(0, 8));
}
