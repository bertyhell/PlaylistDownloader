using System;
using System.IO;

namespace PlaylistDownloader
{
    public class RunSettings
    {
        public RunSettings()
        {
            IsDebug = true;
            NormalizedSuffix = "";
            SongsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "PlaylistDownloader");
        }

        public bool IsDebug { get; set; }
        public string YoutubeDlPath { get; set; }
        public string FfmpegPath { get; set; }
        public string SongsFolder { get; set; }
        public string NormalizedSuffix { get; set; }
    }
}
