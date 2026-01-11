using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public abstract class ObfuscationEngineBase : IObfuscationEngine
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    // Template Method pattern kabi ishlatish mumkin
    public void Execute(ObfuscationContext context)
    {
        Console.WriteLine($"[Engine] {Name} boshlandi: {Description}");
        Process(context);
        Console.WriteLine($"[Engine] {Name} yakunlandi.");
    }

    // Voris klasslar faqat shu metodni yozadi
    protected abstract void Process(ObfuscationContext context);

    // Yordamchi metod: Tasodifiy nom yaratish
    protected string GetRandomName() => "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
}
