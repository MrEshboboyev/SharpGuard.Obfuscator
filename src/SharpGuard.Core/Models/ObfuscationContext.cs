using dnlib.DotNet;

namespace SharpGuard.Core.Models;

public class ObfuscationContext(string inputPath, string outputPath)
{
    // Asosiy .NET moduli (DLL yoki EXE)
    public ModuleDefMD Module { get; set; } = ModuleDefMD.Load(inputPath); // Faylni dnlib yordamida xotiraga yuklash

    // Kirish fayli va chiqish fayli yo'llari
    public string InputPath { get; set; } = inputPath;
    public string OutputPath { get; set; } = outputPath;

    // Nomlarni o'zgartirishda takrorlanmaslikni ta'minlash uchun lug'at
    // Kalit: Eski nom, Qiymat: Yangi chalkash nom
    public Dictionary<string, string> NameMap { get; } = [];

    // Obfuskatsiya qilinmasligi kerak bo'lgan klass/metodlar ro'yxati
    public HashSet<string> ExcludeList { get; } = [];

    public void Save()
    {
        // O'zgarishlarni yangi faylga yozish
        Module.Write(OutputPath);
    }
}
