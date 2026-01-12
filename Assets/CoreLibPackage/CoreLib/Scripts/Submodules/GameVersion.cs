// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: GameVersion.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Represents and manages detailed version information for the game,
//              including release, major, minor, patch, and build hash identifiers.
// ========================================================

using System;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// Represents a specific version of the game, encapsulating version components
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        #region Fields

        public readonly int release;
        public readonly int major;
        public readonly int minor;
        public readonly int patch;

        public readonly string buildHash;

        /// Represents an uninitialized game version (0.0.0.0-).
        public static readonly GameVersion Zero = new(0, 0, 0, 0, string.Empty);

        #endregion

        #region Constructors

        /// Initializes a new instance of the <see cref="GameVersion"/> struct from a formatted version string.
        /// <param name="versionString">
        /// A version string formatted as <c>release.major.minor[.patch]-buildHash</c>.
        /// Example: <c>"1.1.2.0-7da5"</c>
        /// </param>
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
                patch = 0;
                if (versionNumbers.Length > 3)
                    patch = int.Parse(versionNumbers[3]);
            }
            catch (Exception)
            {
               CoreLibMod.Log.LogWarning($"Version string '{versionString}' is not valid!");
               buildHash = "";
               release = 0;
               major = 0;
               minor = 0;
               patch = 0;
            }
        }

        /// Initializes a new instance of the <see cref="GameVersion"/>
        /// <param name="release">The release version number (major lifecycle).</param>
        /// <param name="major">The major version component (significant feature changes).</param>
        /// <param name="minor">The minor version component (incremental content updates).</param>
        /// <param name="patch">The patch version component (small fixes or adjustments).</param>
        /// <param name="buildHash">A unique build hash string identifying the specific game build.</param>
        /// <seealso cref="GameVersion(string)"/>
        public GameVersion(int release, int major, int minor, int patch = 0, string buildHash = "")
        {
            this.release = release;
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.buildHash = buildHash;
        }

        #endregion

        #region Comparison and Equality

        /// Determines whether this version is compatible with another version.
        /// Compatibility is based on identical release, major, and minor components.
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
                   patch == other.patch &&
                   buildHash == other.buildHash;
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

        #endregion

        #region Formatting

        /// Returns the version as a formatted string.
        public override string ToString()
        {
            return $"{release}.{major}.{minor}.{patch}-{buildHash}";
        }

        #endregion
    }
}
