using SharpGuard.Core.Models;

namespace SharpGuard.Core.Engines;

public class RenamingEngine : ObfuscationEngineBase
{
    public override string Name => "Renaming";
    public override string Description => "Metadata nomlarini (Class, Method) chalkashtirish";

    protected override void Process(ObfuscationContext context)
    {
        foreach (var type in context.Module.GetTypes())
        {
            if (type.IsGlobalModuleType || type.IsRuntimeSpecialName) continue;

            // Klass nomini o'zgartirish
            type.Name = GetRandomName();

            foreach (var method in type.Methods)
            {
                if (method.IsRuntimeSpecialName) continue;
                method.Name = GetRandomName();
            }
        }
    }
}
