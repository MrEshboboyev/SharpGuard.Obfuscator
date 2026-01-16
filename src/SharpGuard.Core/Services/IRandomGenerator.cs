using System.Security.Cryptography;

namespace SharpGuard.Core.Services;

/// <summary>
/// Random generator abstraction
/// </summary>
public interface IRandomGenerator
{
    int Next(int min, int max);
    byte[] NextBytes(int count);
    string NextString(int length);
}

/// <summary>
/// Cryptographically secure random generator
/// Implements Adapter pattern for RNG
/// </summary>
public class SecureRandomGenerator : IRandomGenerator
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public int Next(int min, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        if (min == max)
            return min;

        var range = (long)max - min;
        var bytes = new byte[4];
        _rng.GetBytes(bytes);

        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)((value % range) + min);
    }

    public byte[] NextBytes(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var bytes = new byte[count];
        _rng.GetBytes(bytes);
        return bytes;
    }

    public string NextString(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = NextBytes(length);
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }

    public double NextDouble()
    {
        var bytes = new byte[8];
        _rng.GetBytes(bytes);
        var value = BitConverter.ToUInt64(bytes, 0);
        return (double)value / ulong.MaxValue;
    }
}
