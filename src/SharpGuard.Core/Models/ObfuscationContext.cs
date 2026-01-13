using dnlib.DotNet;

namespace SharpGuard.Core.Models;

public class ObfuscationContext(string inputPath, string outputPath)
{
    public ModuleDefMD Module { get; set; } = ModuleDefMD.Load(inputPath); 
    
    public string InputPath { get; set; } = inputPath;
    public string OutputPath { get; set; } = outputPath;

    public Dictionary<string, string> NameMap { get; } = [];

    public HashSet<string> ExcludeList { get; } = [];

    public void Save()
    {
        Module.Write(OutputPath);
    }
}
