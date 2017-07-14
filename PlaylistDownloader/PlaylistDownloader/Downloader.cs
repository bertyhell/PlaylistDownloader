using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace PlaylistDownloader
{
    public class Downloader : BackgroundWorker
    {

        private readonly ICollection<PlaylistItem> _playlist;
        private readonly bool _isDebugMode = bool.Parse(ConfigurationManager.AppSettings.Get("debug"));

        //[download]   0.9% of 3.45MiB at 553.57KiB/s ETA 00:06
        private readonly Regex _extractDownloadProgress = new Regex(@"\[download\][\s]*([0-9\.]+)%");
        private int _progress;
        private readonly int _totalSongs;
        private CancellationTokenSource _cts;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Downloader(ICollection<PlaylistItem> playlist)
        {
            _playlist = playlist;
            _totalSongs = _playlist.Count;

            _cts = new CancellationTokenSource();
        }

        protected override void OnDoWork(DoWorkEventArgs args)
        {
            _progress = 0;

            //setup cancelation
            ParallelOptions po = new ParallelOptions
            {
                CancellationToken = _cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            try
            {
                // Parallal execution
                try
                {
                    Parallel.ForEach(_playlist, po, item =>
                    {
                        try
                        {
                            DownloadPlaylistItem(item, po);
                            //ConvertPlaylistItem(item, po);
                        }
                        catch (InvalidOperationException) { } //ignore exceptions when aborting download
                        catch (Win32Exception) { } //ignore process exception if killed during process exiting
                    });
                }
                catch (OperationCanceledException) { }
                finally
                {
                    if (_cts != null)
                    {
                        _cts.Dispose();
                        _cts = null;
                    }
                }
                // Serial execution
                //foreach (PlaylistItem item in _playlist)
                //{
                //    try
                //    {
                //        DownloadPlaylistItem(item, po);
                //        //ConvertPlaylistItem(item, po);
                //    }
                //    catch (InvalidOperationException) { } //ignore exceptions when aborting download
                //    catch (Win32Exception) { } //ignore process exception if killed during process exiting
                //}
            }
            catch (OperationCanceledException) { } //ignore exceptions caused by canceling paralel.foreach loop
        }

        private void DownloadPlaylistItem(PlaylistItem item, ParallelOptions po)
        {
            po.CancellationToken.ThrowIfCancellationRequested();

            item.DownloadProgress = 5;
            if (!string.IsNullOrWhiteSpace(item.Name))
            {

                YoutubeLink youtubeLink = YoutubeSearcher.GetYoutubeLinks(item.Name).FirstOrDefault();
                item.FileName = MakeValidFileName(youtubeLink.Label);

                if (!File.Exists(SettingsWindow.SONGS_FOLDER + "/" + item.FileName + ".mp3"))
                {
                    item.DownloadProgress = 10;

                    if (youtubeLink == null)
                    {
                        item.SetDownloadStatus(false);
                    }
                    else
                    {
                        string youtubeDlPath = Debugger.IsAttached
                            ? "./youtube-dl.exe"
                            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlaylistDownloader", "youtube-dl.exe");

                        // TODO execute this after download in speparate Process 
                        // "&& .\\ffmpeg\\ffmpeg.exe -i {0}.mp3 -af loudnorm=I=-16:TP=-1.5:LRA=11 -ar 48k test.mp3"
                        Process youtubeDownloadProcess = new Process
                        {
                            StartInfo =
                            {
                                FileName = youtubeDlPath,
                                Arguments = string.Format(" --ffmpeg-location ./ffmpeg" +
                                                          " --extract-audio" +
                                                          " --audio-format mp3" +
                                                          " --output \"{2}\\{0}.%(ext)s\" {1}", item.FileName, youtubeLink.Url, SettingsWindow.SONGS_FOLDER),
                                CreateNoWindow = !_isDebugMode,
                                WindowStyle = _isDebugMode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false
                            }
                        };

                        youtubeDownloadProcess.ErrorDataReceived += Process_ErrorDataReceived; ;
                        youtubeDownloadProcess.Start();

                        //extract progress from process
                        using (StreamReader reader = youtubeDownloadProcess.StandardOutput)
                        {
                            while (!youtubeDownloadProcess.HasExited)
                            {
                                string consoleLine = reader.ReadLine();
                                logger.Info(consoleLine);

                                if (!string.IsNullOrWhiteSpace(consoleLine))
                                {
                                    Match match = _extractDownloadProgress.Match(consoleLine);
                                    if (match.Length > 0 && match.Groups.Count >= 2)
                                    {
                                        double downloadProgress;
                                        if (double.TryParse(match.Groups[1].Value, out downloadProgress))
                                        {
                                            item.DownloadProgress = (int)(10 + downloadProgress / 100 * 50);
                                        }
                                    }
                                }
                                if (CancellationPending)
                                {
                                    logger.Info("Canceling youtube downloader because of user.");
                                    youtubeDownloadProcess.Close();
                                }
                            }
                        }

                        Thread.Sleep(1000);
                    }
                }
            }

            item.DownloadProgress = 100;
            _progress++;
            OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / _totalSongs, null));
        }

        internal void Abort()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
            CancelAsync();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            logger.Error(e.Data);
        }

        private static string MakeValidFileName(string name)
        {
            return Regex.Replace(
                name,
                "[\\W-]+",  /*Matches any nonword character. Equivalent to '[^A-Za-z0-9_]'*/
                "-",
                RegexOptions.IgnoreCase).Trim('-', ' ');
        }
    }
}
