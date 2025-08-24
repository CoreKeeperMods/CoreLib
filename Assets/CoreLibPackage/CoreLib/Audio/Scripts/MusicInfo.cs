// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// <summary>
    /// Represents information related to music files, including paths for the main music and its introduction.
    /// </summary>
    public class MusicInfo
    {
        /// <summary>
        /// Represents the file path to the music file associated with the MusicInfo instance.
        /// </summary>
        public string MusicPath;

        /// <summary>
        /// Represents the file path to the introductory segment of an audio track.
        /// </summary>
        public string IntroPath;
    }
}