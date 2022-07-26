using System;

namespace CoreLib;

public readonly struct GameVersion
{
    public readonly int major;
    public readonly int minor;
    public readonly int patch;
    public readonly string buildHash;

    public GameVersion(string versionString)
    {
        try
        {
            string[] parts = versionString.Split("-");
            string[] versionNumbers = parts[0].Split(".");

            buildHash = parts[1];
            major = int.Parse(versionNumbers[0]);
            minor = int.Parse(versionNumbers[1]);
            patch = int.Parse(versionNumbers[2]);
        }
        catch (Exception)
        {
            throw new ArgumentException($"Version string '{versionString}' is not valid!");
        }
    }

    public GameVersion(int major, int minor, int patch, string buildHash)
    {
        this.major = major;
        this.minor = minor;
        this.patch = patch;
        this.buildHash = buildHash;
    }

    public bool Equals(GameVersion other)
    {
        return major == other.major && minor == other.minor && patch == other.patch && buildHash == other.buildHash;
    }

    public override bool Equals(object obj)
    {
        return obj is GameVersion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(major, minor, patch, buildHash);
    }

    public static bool operator ==(GameVersion left, GameVersion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GameVersion left, GameVersion right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{major}.{minor}.{patch}-{buildHash}";
    }
}