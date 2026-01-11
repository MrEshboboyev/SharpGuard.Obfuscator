using SharpGuard.Core.Helpers;
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

            type.Name = Randomizer.GenerateName(12, Randomizer.NamingScheme.Confusing);

            foreach (var method in type.Methods)
            {
                // Konstruktorlarni yoki runtime metodlarini o'zgartirmaymiz
                if (method.IsRuntimeSpecialName) continue;

                if (context.Module.EntryPoint != null && context.Module.EntryPoint == method)
                    continue;

                // Metod nomini o'zgartirish
                method.Name = Randomizer.GenerateName(10, Randomizer.NamingScheme.Confusing);
            }
        }
    }
}
