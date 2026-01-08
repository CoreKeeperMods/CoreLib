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
    /// <summary>
    /// Represents a specific version of the game, encapsulating version components such as release,
    /// major, minor, patch, and a unique build hash identifier.
    /// Provides comparison, equality, and compatibility checks for version validation.
    /// </summary>
    /// <remarks>
    /// The <see cref="GameVersion"/> struct is primarily used by the Core Library mod to ensure that
    /// the mod is compatible with the current Core Keeper build version. It supports comparison through
    /// <see cref="Equals(GameVersion)"/> and compatibility checks via <see cref="CompatibleWith(GameVersion)"/>.
    /// </remarks>
    /// <seealso cref="CoreLibMod"/>
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        #region Fields

        /// <summary>
        /// The release component of the version number.
        /// Typically, it represents a major lifecycle milestone of the game.
        /// </summary>
        private readonly int _release;

        /// <summary>
        /// The major component of the version number.
        /// Used for large-scale feature additions or major updates.
        /// </summary>
        private readonly int _major;

        /// <summary>
        /// The minor component of the version number.
        /// Indicates smaller content updates or incremental improvements.
        /// </summary>
        private readonly int _minor;

        /// <summary>
        /// The patch component of the version number.
        /// Represents bug fixes or micro updates within a given minor version.
        /// </summary>
        private readonly int _patch;

        /// <summary>
        /// The build hash string uniquely identifying the compiled game build.
        /// This may differ even if version numbers match, representing internal build identifiers.
        /// </summary>
        private readonly string _buildHash;

        /// <summary>
        /// Represents an uninitialized or default game version (0.0.0.0-).
        /// This constant is used when a valid version cannot be parsed or determined.
        /// </summary>
        /// <seealso cref="GameVersion(int,int,int,int,string)"/>
        public static readonly GameVersion Zero = new(0, 0, 0, 0, string.Empty);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GameVersion"/> struct from a formatted version string.
        /// </summary>
        /// <param name="versionString">
        /// A version string formatted as <c>release.major.minor.patch-buildHash</c>.
        /// Example: <c>"1.1.2.0-7da5"</c>
        /// </param>
        /// <remarks>
        /// If the provided string is invalid or cannot be parsed, a warning will be logged and
        /// this instance will default to <c>0.0.0.0-</c>.
        /// </remarks>
        /// <seealso cref="Regex"/>
        public GameVersion(string versionString)
        {
            const string pattern = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))-([a-zA-Z0-9]+)$";
            var match = Regex.Match(versionString, pattern);

            if (match.Success)
            {
                _release = int.Parse(match.Groups[1].Value);
                _major = int.Parse(match.Groups[2].Value);
                _minor = int.Parse(match.Groups[3].Value);
                _patch = int.Parse(match.Groups[4].Value);
                _buildHash = match.Groups[5].Value;
            }
            else
            {
                CoreLibMod.Log.LogWarning($"Version string '{versionString}' is not valid!");
                _release = 0;
                _major = 0;
                _minor = 0;
                _patch = 0;
                _buildHash = string.Empty;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameVersion"/> struct using explicit numeric values.
        /// </summary>
        /// <param name="release">The release version number (major lifecycle).</param>
        /// <param name="major">The major version component (significant feature changes).</param>
        /// <param name="minor">The minor version component (incremental content updates).</param>
        /// <param name="patch">The patch version component (small fixes or adjustments).</param>
        /// <param name="buildHash">A unique build hash string identifying the specific game build.</param>
        /// <seealso cref="GameVersion(string)"/>
        public GameVersion(int release, int major, int minor, int patch = 0, string buildHash = "")
        {
            _release = release;
            _major = major;
            _minor = minor;
            _patch = patch;
            _buildHash = buildHash;
        }

        #endregion

        #region Comparison and Equality

        /// <summary>
        /// Determines whether this version is compatible with another version.
        /// Compatibility is based on identical release, major, and minor components.
        /// </summary>
        /// <param name="other">The other <see cref="GameVersion"/> to compare against.</param>
        /// <returns>
        /// <c>true</c> if both versions share the same release, major, and minor numbers; otherwise, <c>false</c>.
        /// </returns>
        /// <seealso cref="Equals(GameVersion)"/>
        public bool CompatibleWith(GameVersion other)
        {
            return _release == other._release &&
                   _major == other._major &&
                   _minor == other._minor;
        }

        /// <summary>
        /// Determines whether the current version instance is equal to another <see cref="GameVersion"/>.
        /// </summary>
        /// <param name="other">The <see cref="GameVersion"/> to compare to.</param>
        /// <returns><c>true</c> if all components (including build hash) match; otherwise, <c>false</c>.</returns>
        /// <seealso cref="object.Equals(object)"/>
        public bool Equals(GameVersion other)
        {
            return _release == other._release &&
                   _major == other._major &&
                   _minor == other._minor &&
                   _patch == other._patch &&
                   _buildHash == other._buildHash;
        }

        /// <summary>
        /// Determines whether the specified object represents the same version as the current instance.
        /// </summary>
        /// <param name="obj">An object to compare with the current <see cref="GameVersion"/>.</param>
        /// <returns><c>true</c> if the object is a <see cref="GameVersion"/> and represents the same version; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is GameVersion other && Equals(other);
        }

        /// <summary>
        /// Generates a hash code for the current <see cref="GameVersion"/> instance.
        /// </summary>
        /// <returns>An integer hash code based on the release, major, and minor components.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(_release, _major, _minor);
        }

        /// <summary>
        /// Equality operator for comparing two <see cref="GameVersion"/> instances.
        /// </summary>
        /// <param name="left">The left-hand <see cref="GameVersion"/> operand.</param>
        /// <param name="right">The right-hand <see cref="GameVersion"/> operand.</param>
        /// <returns><c>true</c> if both represent identical versions; otherwise, <c>false</c>.</returns>
        public static bool operator ==(GameVersion left, GameVersion right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for comparing two <see cref="GameVersion"/> instances.
        /// </summary>
        /// <param name="left">The left-hand <see cref="GameVersion"/> operand.</param>
        /// <param name="right">The right-hand <see cref="GameVersion"/> operand.</param>
        /// <returns><c>true</c> if the versions differ; otherwise, <c>false</c>.</returns>
        public static bool operator !=(GameVersion left, GameVersion right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Returns the version as a formatted string.
        /// </summary>
        /// <returns>
        /// A formatted string representation: <c>release.major.minor.patch-buildHash</c>.
        /// Example: <c>1.1.2.0-7da5</c>
        /// </returns>
        public override string ToString()
        {
            return $"{_release}.{_major}.{_minor}.{_patch}-{_buildHash}";
        }

        #endregion
    }
}
