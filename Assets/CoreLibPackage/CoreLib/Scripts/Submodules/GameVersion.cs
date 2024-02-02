using System;

namespace CoreLib
{
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        public readonly int release;
        public readonly int major;
        public readonly int minor;
        public readonly string buildHash;

        public static GameVersion zero = new GameVersion(0, 0, 0, "");

        public GameVersion(string versionString)
        {
            try
            {
                string[] parts = versionString.Split('-');
                string[] versionNumbers = parts[0].Split('.');

                buildHash = parts[1];
                release = int.Parse(versionNumbers[0]);
                major = int.Parse(versionNumbers[1]);
                minor = int.Parse(versionNumbers[2]);
            }
            catch (Exception)
            {
               CoreLibMod.Log.LogWarning($"Version string '{versionString}' is not valid!");
               buildHash = "";
               release = 0;
               major = 0;
               minor = 0;
            }
        }

        public GameVersion(int release, int major, int minor, string buildHash)
        {
            this.release = release;
            this.major = major;
            this.minor = minor;
            this.buildHash = buildHash;
        }
        
        [Obsolete("Patch was removed")]
        public GameVersion(int release, int major, int minor, int patch, string buildHash)
        {
            this.release = release;
            this.major = major;
            this.minor = minor;
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
                   minor == other.minor;
        }

        public override bool Equals(object obj)
        {
            return obj is GameVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(release, major, minor);
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
            return $"{release}.{major}.{minor}-{buildHash}";
        }
    }
}