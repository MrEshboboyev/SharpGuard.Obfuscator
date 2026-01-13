using System.Text;

namespace SharpGuard.Core.Helpers;

public static class Randomizer
{
    private static readonly Random _random = new();

    public enum NamingScheme
    {
        Alphanumeric,   // aB12...
        Confusing,      // lIIll1I... (most hard readable)
        Invisible,      // invisible unicode characters
        Simple          // a, b, c...
    }

    private static readonly Dictionary<NamingScheme, string> _charSets = new()
    {
        { NamingScheme.Alphanumeric, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" },
        { NamingScheme.Confusing, "lI1i|" },
        { NamingScheme.Simple, "abcdefghijklmnopqrstuvwxyz" }
    };

    private static readonly string[] _invisibleChars = { "\u200B", "\u200C", "\u200D", "\u200E", "\u200F" };

    public static string GenerateName(int length = 10, NamingScheme scheme = NamingScheme.Confusing)
    {
        if (scheme == NamingScheme.Invisible)
        {
            StringBuilder sb = new("_");
            for (int i = 0; i < length; i++)
                sb.Append(_invisibleChars[_random.Next(_invisibleChars.Length)]);

            return sb.ToString();
        }

        string chars = _charSets[scheme];
        char[] identifier = new char[length];

        string letters = new(chars.Where(c => char.IsLetter(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(letters)) letters = "a";

        identifier[0] = letters[_random.Next(letters.Length)];

        for (int i = 1; i < length; i++)
        {
            identifier[i] = chars[_random.Next(chars.Length)];
        }

        return new string(identifier);
    }
}
