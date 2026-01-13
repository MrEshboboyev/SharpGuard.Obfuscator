namespace SharpGuard.CLI;

public class Arguments
{
    public string? InputPath { get; private set; }
    public string? OutputPath { get; private set; }
    public bool Rename { get; private set; } = true;
    public bool EncryptStrings { get; private set; } = false;
    public bool ObfuscateControlFlow { get; private set; } = false;

    public static Arguments Parse(string[] args)
    {
        var parsed = new Arguments();

        if (args.Length == 0) return null!;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

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
                case "--no-rename":
                    parsed.Rename = false;
                    break;
                case "--str":
                case "--string-encrypt":
                    parsed.EncryptStrings = true;
                    break;
                case "--cf":
                case "--control-flow":
                    parsed.ObfuscateControlFlow = true;
                    break;
                default:
                    if (i == 0 && !arg.StartsWith('-')) parsed.InputPath = args[i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(parsed.OutputPath) && !string.IsNullOrEmpty(parsed.InputPath))
        {
            parsed.OutputPath = parsed.InputPath.Replace(".dll", "_protected.dll").Replace(".exe", "_protected.exe");
        }

        return parsed;
    }
}
