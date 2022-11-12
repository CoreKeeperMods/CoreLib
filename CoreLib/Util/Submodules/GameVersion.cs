using System;

namespace CoreLib;

public readonly struct GameVersion
{
    public readonly int release;
    public readonly int major;
    public readonly int minor;
    public readonly int patch;
    public readonly string buildHash;

    public static GameVersion zero = new GameVersion(0, 0, 0, 0, "");

    public GameVersion(string versionString)
    {
        try
        {
            string[] parts = versionString.Split("-");
            string[] versionNumbers = parts[0].Split(".");

            buildHash = parts[1];
            release = int.Parse(versionNumbers[0]);
            major = int.Parse(versionNumbers[1]);
            minor = int.Parse(versionNumbers[2]);
            patch = int.Parse(versionNumbers[3]);
        }
        catch (Exception)
        {
            throw new ArgumentException($"Version string '{versionString}' is not valid!");
        }
    }

    public GameVersion(int release, int major, int minor, int patch, string buildHash)
    {
        this.release = release;
        this.major = major;
        this.minor = minor;
        this.patch = patch;
        this.buildHash = buildHash;
    }

    public bool CompatibleWith(GameVersion other)
    {
        return release == other.release && 
               major == other.major && 
               minor == other.minor;
    }

    public bool Equals(GameVersion other)
    {
        return release == other.release && 
               major == other.major && 
               minor == other.minor && 
               patch == other.patch;
    }

    public override string ToString()
    {
        return $"{release}.{major}.{minor}.{patch}-{buildHash}";
    }
}