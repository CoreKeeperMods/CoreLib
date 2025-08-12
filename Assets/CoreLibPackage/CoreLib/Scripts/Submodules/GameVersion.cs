using System;

namespace CoreLib
{
    /// <summary>
    /// Represents a specific version of the game, including details such as release, major, minor version components,
    /// and a unique build hash for identifying the build.
    /// </summary>
    /// <remarks>
    /// This struct implements <see cref="System.IEquatable{T}"/> for easy comparison between game version instances.
    /// It is immutable, ensuring the integrity of version data across the application.
    /// The versioning system generally follows the format: Release.Major.Minor.
    /// An additional build hash is included to uniquely identify a specific build of the version.
    /// </remarks>
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        /// <summary>
        /// Represents the release version number of the game.
        /// This value indicates the major lifecycle or stage of the game version,
        /// and is the first part of the version format.
        /// </summary>
        public readonly int release;

        /// <summary>
        /// Represents the major version component of the game version.
        /// This value indicates significant revisions or updates
        /// that may include substantial new features or changes.
        /// </summary>
        public readonly int major;

        /// <summary>
        /// Represents the minor version component of the game version identifier.
        /// </summary>
        /// <remarks>
        /// The minor version typically indicates smaller updates, feature additions, or non-breaking changes
        /// that are released after the major version has been incremented.
        /// </remarks>
        public readonly int minor;

        /// <summary>
        /// Represents a unique identifier or hash of the build version in the <see cref="GameVersion"/> structure.
        /// This string differentiates specific builds of the game even when the release, major, and minor versions remain the same.
        /// </summary>
        public readonly string buildHash;

        /// <summary>
        /// Represents a default or uninitialized instance of the <see cref="GameVersion"/> struct.
        /// This value signifies a state where no specific game version is assigned.
        /// </summary>
        public static GameVersion zero = new GameVersion(0, 0, 0, "");

        /// Encapsulates game version details, including release, major, minor, and build hash components.
        /// This structure facilitates version management, validation, and compatibility handling within
        /// the application.
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

        /// Defines and manages the versioning metadata of the game, including release, major, minor,
        /// and build hash details. This structure supports operations such as equality checks and
        /// compatibility validation across different components of the application.
        public GameVersion(int release, int major, int minor, string buildHash)
        {
            this.release = release;
            this.major = major;
            this.minor = minor;
            this.buildHash = buildHash;
        }

        /// Represents a specific game version, including release, major, minor, and build hash components.
        /// Provides methods to handle version equality, compatibility checking, and version string parsing.
        /// Designed to support version management and validation within the application.
        [Obsolete("Patch was removed")]
        public GameVersion(int release, int major, int minor, int patch, string buildHash)
        {
            this.release = release;
            this.major = major;
            this.minor = minor;
            this.buildHash = buildHash;
        }

        /// Determines whether the current game version is compatible with a specified game version.
        /// Compatibility is assessed based on the release, major, and minor version components.
        /// <param name="other">The game version to compare against for compatibility.</param>
        /// <returns>true if the game versions are compatible; otherwise, false.</returns>
        public bool CompatibleWith(GameVersion other)
        {
            return release == other.release && 
                   major == other.major && 
                   minor == other.minor;
        }

        /// Determines whether the current GameVersion instance is equal to another specified GameVersion instance.
        /// <param name="other">The GameVersion instance to compare with the current instance.</param>
        /// <returns>True if the current instance is equal to the specified GameVersion instance; otherwise, false.</returns>
        public bool Equals(GameVersion other)
        {
            return release == other.release &&
                   major == other.major &&
                   minor == other.minor;
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
            return HashCode.Combine(release, major, minor);
        }

        /// Defines a custom operator for the type, enabling specialized behavior for operations such as comparison, arithmetic, or logical evaluation specific to the implementing type.
        /// This enhances the type's functionality and provides an intuitive way to work with its instances within expressions.
        public static bool operator ==(GameVersion left, GameVersion right)
        {
            return left.Equals(right);
        }

        /// Defines a custom operator for a type, enabling specific operations
        /// to be performed on instances of the type in a concise or specialized manner.
        public static bool operator !=(GameVersion left, GameVersion right)
        {
            return !left.Equals(right);
        }

        /// Returns a string representation of the game version, including release, major, minor,
        /// and build hash components in a formatted layout.
        /// <return>
        /// A string that represents the current GameVersion instance, formatted as
        /// "release.major.minor-buildHash".
        /// </return>
        public override string ToString()
        {
            return $"{release}.{major}.{minor}-{buildHash}";
        }
    }
}