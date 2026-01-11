using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public interface IObfuscationEngine
{
    string Name { get; }
    string Description { get; }

    void Execute(ObfuscationContext context);
}
