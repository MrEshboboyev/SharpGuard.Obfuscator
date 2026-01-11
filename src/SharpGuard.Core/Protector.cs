using SharpGuard.Core.Models;

namespace SharpGuard.Core;

public class Protector(
    string inputPath,
    string outputPath
)
{
    private readonly ObfuscationContext _context = new(inputPath, outputPath);

    public void Execute()
    {
        Console.WriteLine($"[+] Obfuskatsiya boshlandi: {_context.Module.Name}");

        // 1. Nomlarni o'zgartirish (Renaming)
        // Kelajakda Engines/RenamingEngine.cs shu yerda chaqiriladi
        RunRenaming();

        // 2. Stringlarni shifrlash (String Encryption)
        RunStringEncryption();

        // Natijani saqlash
        _context.Save();
        Console.WriteLine($"[+] Muvaffaqiyatli yakunlandi: {_context.OutputPath}");
    }

    private void RunRenaming()
    {
        Console.WriteLine("[-] Nomlar o'zgartirilmoqda...");
        // Hozircha bo'sh, keyingi qadamda to'ldiramiz
    }

    private void RunStringEncryption()
    {
        Console.WriteLine("[-] Satrlar shifrlanmoqda...");
        // Hozircha bo'sh
    }
}
