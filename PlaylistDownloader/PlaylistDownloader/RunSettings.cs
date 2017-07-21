using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistDownloader
{
    public class RunSettings
    {
        public RunSettings()
        {
            IsDebug = true;
            SongsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "PlaylistDownloader");
        }

        public bool IsDebug { get; set; }
        public string YoutubeDlPath { get; set; }
        public string FfmpegPath { get; set; }
        public string SongsFolder { get; set; }
    }
}
