using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistDownloader.UnitTests
{
    [TestFixture]
    public class DownloaderTests
    {
        [TestCase("https://www.youtube.com/watch?v=rpIPNEEo6ng", "Top-15-New-Biggest-Upcoming-Open-World-Games-in-2017-2018.mp3")]
        [TestCase("https://www.youtube.com/watch?v=447ZQHns2Jo", "How-to-take-off-and-installing-logitech-illuminated-keyboard-key.mp3")]
        public void Downloader_DownloadYoutubeLink_ShouldSucceed(string url, string fileName)
        {
            //  Arrange
            string currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string relativePathToProject = "..\\..\\..\\PlaylistDownloader\\bin\\debug\\";
            var runSettings = new RunSettings
            {
                IsDebug = true,
                YoutubeDlPath = Path.Combine(currentDirectory, relativePathToProject, "youtube-dl.exe"),
                FfmpegPath = Path.Combine(currentDirectory, relativePathToProject, "ffmpeg\\ffmpeg.exe")
                
            };

            File.Delete(Path.Combine(runSettings.SongsFolder, fileName));

            var playListItem = new PlaylistItem(null)
            {
                Name = url
            };

            var downloader = new Downloader(runSettings, new List<PlaylistItem> { playListItem });

            //  Act
            string filePath = downloader.DownloadPlaylistItem(playListItem);

            //  Assert
            Assert.That(File.Exists(filePath), Is.True);
        }
    }
}
