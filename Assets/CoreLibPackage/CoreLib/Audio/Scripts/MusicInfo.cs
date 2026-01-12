// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: MusicInfo.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Represents metadata for music tracks, including file paths for the main
//              music segment and an optional introductory segment.
// ========================================================

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// Represents information related to a music track, including file paths
    /// for both the main and introductory segments.
    /// <remarks>
    /// Instances of this class are typically used by CoreLib’s audio system to manage
    /// background music with optional intro sections that seamlessly transition into
    /// looping main tracks.
    /// </remarks>
    /// <seealso cref="AudioModule"/>
    public class MusicInfo
    {
        #region Fields

        /// The file path to the main music file associated with this <see cref="MusicInfo"/> instance.
        /// <remarks>
        /// This file typically represents the looping section of a track after any introduction plays.
        /// </remarks>
        public string MusicPath;

        /// The file path to the introductory segment of the audio track, if one exists.
        /// <remarks>
        /// When present, this segment plays before transitioning into the main looping track.
        /// </remarks>
        public string IntroPath;

        #endregion
    }
}