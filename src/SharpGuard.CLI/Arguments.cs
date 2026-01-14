namespace SharpGuard.CLI;

public class Arguments
{
    public string? InputPath { get; private set; }
    public string? OutputPath { get; private set; }
    public string? ConfigPath { get; private set; }
    public string? Level { get; private set; }
    
    public bool DisableRenaming { get; private set; }
    public bool DisableStringEncryption { get; private set; }
    public bool DisableControlFlow { get; private set; }
    public bool DisableAntiDebugging { get; private set; }

    public static Arguments Parse(string[] args)
    {
        var parsed = new Arguments();

        if (args.Length == 0) return null!;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length) parsed.InputPath = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length) parsed.OutputPath = args[++i];
                    break;
                case "-c":
                case "--config":
                    if (i + 1 < args.Length) parsed.ConfigPath = args[++i];
                    break;
                case "-l":
                case "--level":
                    if (i + 1 < args.Length) parsed.Level = args[++i];
                    break;
                case "--no-renaming":
                    parsed.DisableRenaming = true;
                    break;
                case "--no-stringenc":
                    parsed.DisableStringEncryption = true;
                    break;
                case "--no-controlflow":
                    parsed.DisableControlFlow = true;
                    break;
                case "--no-antidebug":
                    parsed.DisableAntiDebugging = true;
                    break;
                default:
                    if (i == 0 && !arg.StartsWith('-')) parsed.InputPath = args[i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(parsed.OutputPath) && !string.IsNullOrEmpty(parsed.InputPath))
        {
            var inputDir = Path.GetDirectoryName(parsed.InputPath);
            var inputName = Path.GetFileNameWithoutExtension(parsed.InputPath);
            var inputExt = Path.GetExtension(parsed.InputPath);
            parsed.OutputPath = Path.Combine(inputDir ?? ".", $"{inputName}_protected{inputExt}");
        }

        return parsed;
    }
}
