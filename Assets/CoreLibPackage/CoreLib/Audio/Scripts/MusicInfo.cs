// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Audio
{
    /// Represents information related to music files, including paths for the main music and its introduction.
    public class MusicInfo
    {
        /// Represents the file path to the music file associated with the MusicInfo instance.
        public string MusicPath;

        /// Represents the file path to the introductory segment of an audio track.
        public string IntroPath;
    }
}