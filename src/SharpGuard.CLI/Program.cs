using SharpGuard.CLI;
using SharpGuard.Core;

Console.WriteLine("=== SharpGuard Obfuscator v1.0 ===");

var parsedArgs = Arguments.Parse(args);
if (parsedArgs == null || string.IsNullOrEmpty(parsedArgs.InputPath))
{
    Console.WriteLine("Qo'llash: SharpGuard.CLI <path> --str --cf");
    return;
}

try
{
    var protector = new Protector(parsedArgs.InputPath, parsedArgs.OutputPath!);
    protector.Execute();
}
catch (Exception ex)
{
    Console.WriteLine($"[!] Xatolik yuz berdi: {ex.Message}");
}
