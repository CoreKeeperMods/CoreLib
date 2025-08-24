using System;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// <summary>
    /// Represents a specific version of the game, including details such as release, major, minor version components,
    /// and a unique build hash for identifying the build.
    /// </summary>
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        public readonly int Release;
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly string BuildHash;

        /// <summary>
        /// Represents a default or uninitialized instance of the <see cref="GameVersion"/> struct.
        /// This value signifies a state where no specific game version is assigned.
        /// </summary>
        /// <returns>{0, 0, 0, ""}</returns>>
        public static GameVersion Zero = new(0, 0, 0);

        /// Encapsulates game version details, including release, major, minor, and build hash components.
        /// This structure facilitates version management, validation, and compatibility handling within
        /// the application.
        public GameVersion(string versionString)
        {
            const string pattern = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))-([a-zA-Z0-9]+)$";
            var match = Regex.Match(versionString, pattern);
            if (match.Success)
            {
                Release = int.Parse(match.Groups[1].Value);
                Major = int.Parse(match.Groups[2].Value);
                Minor = int.Parse(match.Groups[3].Value);
                Patch = int.Parse(match.Groups[4].Value);
                BuildHash = match.Groups[5].Value;
            }
            else
            {
                CoreLibMod.Log.LogWarning($"Version string '{versionString}' is not valid!");
                Release = 0;
                Major = 0;
                Minor = 0;
                Patch = 0;
                BuildHash = "";
            }
        }

        /// Defines and manages the versioning metadata of the game, including release, major, minor,
        /// and build hash details. This structure supports operations such as equality checks and
        /// compatibility validation across different components of the application.
        public GameVersion(int release, int major, int minor, int patch = 0, string buildHash = "")
        {
            Release = release;
            Major = major;
            Minor = minor;
            Patch = patch;
            BuildHash = buildHash;
        }

        /// Determines whether the current game version is compatible with a specified game version.
        /// Compatibility is assessed based on the release, major, and minor version components.
        /// <param name="other">The game version to compare against for compatibility.</param>
        /// <returns>true if the game versions are compatible; otherwise, false.</returns>
        public bool CompatibleWith(GameVersion other)
        {
            return Release == other.Release && 
                   Major == other.Major && 
                   Minor == other.Minor;
        }

        /// Determines whether the current GameVersion instance is equal to another specified GameVersion instance.
        /// <param name="other">The GameVersion instance to compare with the current instance.</param>
        /// <returns>True if the current instance is equal to the specified GameVersion instance; otherwise, false.</returns>
        public bool Equals(GameVersion other)
        {
            return Release == other.Release &&
                   Major == other.Major &&
                   Minor == other.Minor &&
                   BuildHash == other.BuildHash;
        }

        /// Determines whether the specified object is equal to the current GameVersion instance.
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// True if the specified object is a GameVersion and has the same release, major, minor,
        /// and buildHash values as the current instance; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is GameVersion other && Equals(other);
        }

        /// Provides a hash code for the current `GameVersion` instance using its `release`, `major`, and `minor` fields.
        /// The hash code is computed uniquely to identify this instance based on its version numbers.
        /// <returns>
        /// An integer hash code for the current `GameVersion` instance.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Release, Major, Minor);
        }

        /// Defines a custom operator for the type, enabling specialized behavior for operations such as comparison, arithmetic, or logical evaluation specific to the implementing type.
        /// This enhances the type's functionality and provides an intuitive way to work with its instances within expressions.
        public static bool operator ==(GameVersion left, GameVersion right)
        {
            return left.Equals(right);
        }

        /// Defines a custom operator for a type, enabling specific operations
        /// <returns><see cref="Boolean"/></returns>
        /// <example>0.0.0-7ab != 0.0.0-7ac</example>
        public static bool operator !=(GameVersion left, GameVersion right)
        {
            return !left.Equals(right);
        }

        /// Returns a string representation of the game version, including release, major, minor,
        /// and build hash components in the following formatted layout: "<see cref="Release"/>.<see cref="Major"/>.<see cref="Minor"/>-<see cref="BuildHash"/>".
        public override string ToString()
        {
            return $"{Release}.{Major}.{Minor}-{BuildHash}";
        }
    }
}