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
                    Parallel.ForEach(_playlist, po, async item =>
                    {
                        try
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();
                            await DownloadPlaylistItem(item);
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

        public async Task<string> DownloadPlaylistItem(PlaylistItem item)
        {
            string destinationFilePathWithoutExtension = null;
            string tempFilePathWithoutExtension = null;

            item.DownloadProgress = 5;
            if (!string.IsNullOrWhiteSpace(item.Name))
            {

                YoutubeLink youtubeLink = YoutubeSearcher.GetYoutubeLinks(item.Name).FirstOrDefault();
                item.FileName = MakeValidFileName(youtubeLink.Label);
                item.FileName = string.Concat(youtubeLink.Label.Split(Path.GetInvalidFileNameChars())).Trim().Replace('–', '-');
                var workingFolder = Path.GetTempPath();
                tempFilePathWithoutExtension = Path.Combine(Path.GetTempPath(), item.FileName);
                destinationFilePathWithoutExtension = Path.Combine(_runSettings.SongsFolder, item.FileName);

                if (!File.Exists(destinationFilePathWithoutExtension + ".mp3"))
                {
                    item.DownloadProgress = 10;

                    if (youtubeLink == null)
                    {
                        item.SetDownloadStatus(false);
                    }
                    else
                    {
                        // Download videoand extract the mp3 file
                        await StartProcess(
                            _runSettings.YoutubeDlPath,
                            string.Format(" --ffmpeg-location \"{0}\"" +
                                          " --format bestaudio[ext=mp3]/best" +
                                          " --audio-quality 0" +
                                          " --no-part" +
                                          " --extract-audio" +
                                          " --audio-format mp3" +
                                          " --output \"{1}\"" +
                                          " {2}", _runSettings.FfmpegPath, tempFilePathWithoutExtension + "-raw.%(ext)s", youtubeLink.Url),
                            item,
                            ParseYoutubeDlProgress);
                        // -o "c:\Users\Julian\Music\PlaylistDownloader\\%(title)s.%(ext)s"

                        // Normalize audio file after the youtube-dl process has exited
                        await StartProcess(_runSettings.FfmpegPath,
                            string.Format(" -i \"{0}\"" +
                                          " -af loudnorm=I=-16:TP=-1.5:LRA=11" +
                                          //" -ar 48k" +
                                          " -y" +
                                          " \"{1}\"", tempFilePathWithoutExtension + "-raw.mp3", tempFilePathWithoutExtension + _runSettings.NormalizedSuffix + ".mp3"),
                            item,
                            ParseYoutubeDlProgress);

                        // move to destination
                        File.Move(tempFilePathWithoutExtension + _runSettings.NormalizedSuffix + ".mp3",
                            destinationFilePathWithoutExtension + _runSettings.NormalizedSuffix + ".mp3");

                        // Delete the non normalized file after completion if not in debug mode
                        File.Delete(Path.Combine(_runSettings.SongsFolder, item.FileName + "-raw.mp3"));
                    }


                }
            }

            item.DownloadProgress = 100;
            _progress++;
            OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / _totalSongs, null));

            

            return destinationFilePathWithoutExtension;
        }

        private void ParseYoutubeDlProgress(string consoleLine, PlaylistItem item)
        {
            // [download]   0.0% of 4.66MiB at 336.14KiB/s ETA 00:14
            Regex extractDownloadProgress = new Regex(@"\[download\][\s]*([0-9\.]+)%");
            Match match = extractDownloadProgress.Match(consoleLine);
            if (match.Length > 0 && match.Groups.Count >= 2)
            {
                if (double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double downloadProgress))
                {
                    logger.Info("[download + convert progress] " + downloadProgress);
                    if (downloadProgress > 100 || _progress > 100)
                    {
                        Debugger.Break();
                    }
                    item.DownloadProgress = (int)(10.0 + downloadProgress / 100 * 60);
                    OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / _totalSongs, null));
                }
            }
        }

        private void ParseNormalizeProgress(string consoleLine, PlaylistItem item)
        {
            // Duration: 00:30:58.39, start: 0.023021, bitrate: 106 kb/s
            // size=     105kB time=00:00:06.69 bitrate= 128.7kbits/s speed=13.4x 
            Regex extractDuration = new Regex(@"Duration:\s*([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2}),\sstart:");
            Match match = extractDuration.Match(consoleLine);
            if (match.Length > 0 && match.Groups.Count >= 2)
            {
                logger.Info("Duration: " + match.Groups[0].Value);
                item.Duration = TimeSpan.Parse(match.Groups[0].Value).TotalSeconds;
                logger.Info("Duration seconds: " + item.Duration);
                return;
            }

            if (item.Duration == 0)
            {
                return;
            }

            Regex extractProgressDuration = new Regex(@"time=([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2})\s*bitrate=");
            match = extractProgressDuration.Match(consoleLine);
            if (match.Length > 0 && match.Groups.Count >= 2)
            {
                logger.Info("progress Duration: " + match.Groups[0].Value);
                logger.Info("progress Duration seconds: " + TimeSpan.Parse(match.Groups[0].Value).TotalSeconds);
                item.DownloadProgress = (int)(70 + TimeSpan.Parse(match.Groups[0].Value).TotalSeconds / item.Duration * 30);
                OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / _totalSongs, null));
            }
        }

        private Task<string> StartProcess(string executablePath, string arguments, PlaylistItem item, Action<string, PlaylistItem> parseProgressFunc)
        {
            var promise = new TaskCompletionSource<string>();
            logger.Info("[RUN CMD] " + executablePath + arguments);
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

                },
                EnableRaisingEvents = true
            };
            // --audio-quality 5 --extract-audio --audio-format mp3  -o "c:\Users\Julian\Music\PlaylistDownloader\\%(title)s.%(ext)s" https://www.youtube.com/watch?v=mDuElaL1dU0

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                string consoleLine = e.Data;
                if (!string.IsNullOrWhiteSpace(consoleLine))
                {
                    logger.Info(consoleLine);
                    parseProgressFunc(consoleLine, item);
                }

                if (CancellationPending)
                {
                    logger.Info("Canceling process because of user: " + executablePath);
                    process.Close();
                    promise.SetResult(null);
                }
            };

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                string consoleLine = e.Data;

                if (!string.IsNullOrWhiteSpace(consoleLine))
                {
                    logger.Info("Error: " + consoleLine);
                    parseProgressFunc(consoleLine, item);
                }

                if (CancellationPending)
                {
                    logger.Info("Canceling process because of user: " + executablePath);
                    process.Close();
                    promise.SetResult(null);
                }
            };

            process.Exited += new EventHandler((object sender, EventArgs e) =>
            {
                process.Dispose();
                logger.Info("Closing process");
                promise.SetResult(null);
            });

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return promise.Task;
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
                "[^A-Za-z0-9_ -]+",  /*Matches any nonword character. Equivalent to '[^A-Za-z0-9_]'*/
                "-",
                RegexOptions.IgnoreCase).Trim('-', ' ').ToLower();
        }
    }
}
