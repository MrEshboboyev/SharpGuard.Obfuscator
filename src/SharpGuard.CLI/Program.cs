using SharpGuard.Core;

Console.WriteLine("=== SharpGuard Obfuscator v1.0 ===");

if (args.Length < 1)
{
    Console.WriteLine("Foydalanish: SharpGuard.CLI <file_path>");
    return;
}

string input = args[0];
string output = input.Replace(".dll", "_protected.dll").Replace(".exe", "_protected.exe");

try
{
    var protector = new Protector(input, output);
    protector.Execute();
}
catch (Exception ex)
{
    Console.WriteLine($"[!] Xatolik yuz berdi: {ex.Message}");
}
