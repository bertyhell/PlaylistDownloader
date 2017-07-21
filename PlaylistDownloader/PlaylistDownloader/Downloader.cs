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
        private readonly RunSettings _runSettings;

        //[download]   0.9% of 3.45MiB at 553.57KiB/s ETA 00:06
        private readonly Regex _extractDownloadProgress = new Regex(@"\[download\][\s]*([0-9\.]+)%");
        private int _progress;
        private readonly int _totalSongs;
        private CancellationTokenSource _cts;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Downloader(RunSettings settings, ICollection<PlaylistItem> playlist)
        {
            _playlist = playlist;
            _totalSongs = _playlist.Count;

            _cts = new CancellationTokenSource();

            _runSettings = settings;
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
                            po.CancellationToken.ThrowIfCancellationRequested();
                            DownloadPlaylistItem(item);
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

        public string DownloadPlaylistItem(PlaylistItem item)
        {
            string filePath = null;

            item.DownloadProgress = 5;
            if (!string.IsNullOrWhiteSpace(item.Name))
            {

                YoutubeLink youtubeLink = YoutubeSearcher.GetYoutubeLinks(item.Name).FirstOrDefault();
                item.FileName = MakeValidFileName(youtubeLink.Label);
                filePath = Path.Combine(_runSettings.SongsFolder, item.FileName + ".mp3");

                if (!File.Exists(filePath))
                {
                    item.DownloadProgress = 10;

                    if (youtubeLink == null)
                    {
                        item.SetDownloadStatus(false);
                    }
                    else
                    {

                        // Download videoand extract the mp3 file
                        Process youtubeDownloadProcess = StartProcess(
                            _runSettings.YoutubeDlPath,
                            string.Format(" --ffmpeg-location \"{3}\"" +
                                          " --extract-audio" +
                                          " --audio-format mp3" +
                                          " --output \"{2}\\{0}.%(ext)s\" {1}", item.FileName, youtubeLink.Url, _runSettings.SongsFolder, _runSettings.FfmpegPath));

                        Thread.Sleep(1000);

                        // Normalize audio file after the youtube-dl process has exited
                        StartProcess(_runSettings.FfmpegPath, string.Format(" -i \"{0}\"" +
                                                               " -af loudnorm=I=-16:TP=-1.5:LRA=11" +
                                                               " -ar 48k" +
                                                               " -y" +
                                                               " \"{0}\"", filePath));

                        Thread.Sleep(1000);
                    }
                }
            }

            item.DownloadProgress = 100;
            _progress++;
            OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / _totalSongs, null));

            return filePath;
        }

        private Process StartProcess(string executablePath, string arguments)
        {
            Process process = new Process
            {
                StartInfo =
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    CreateNoWindow = !_runSettings.IsDebug,
                    WindowStyle = _runSettings.IsDebug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            //process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine("event error: " + e.Data);
            //process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine("event output: " + e.Data);
            process.Start();

            using (StreamReader errorReader = process.StandardError)
            using (StreamReader outputReader = process.StandardOutput)
            {
                while (!process.HasExited || !outputReader.EndOfStream || !errorReader.EndOfStream)
                {
                    if (!outputReader.EndOfStream)
                    {
                        string consoleLine = outputReader.ReadLine();
                        Debug.WriteLine(consoleLine);
                        logger.Info(consoleLine);
                    }

                    if (!errorReader.EndOfStream)
                    {
                        string consoleLine = errorReader.ReadLine();
                        Debug.WriteLine("Error: " + consoleLine);
                        logger.Error(consoleLine);
                    }

                    //if (!string.IsNullOrWhiteSpace(consoleLine))
                    //{
                    //    Match match = _extractDownloadProgress.Match(consoleLine);
                    //    if (match.Length > 0 && match.Groups.Count >= 2)
                    //    {
                    //        if (double.TryParse(match.Groups[1].Value, out double downloadProgress))
                    //        {
                    //            item.DownloadProgress = (int)(startPercentage + downloadProgress / 100 * endPercentage);
                    //        }
                    //    }
                    //}
                    //if (CancellationPending)
                    //{
                    //    logger.Info("Canceling process because of user: " + executablePath);
                    //    process.Close();
                    //}
                }
            }

            return process;
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
