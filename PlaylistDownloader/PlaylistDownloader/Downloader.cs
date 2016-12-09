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
		private readonly CancellationTokenSource _cts;


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
			item.DownloadProgress = 5;
			if (!string.IsNullOrWhiteSpace(item.Name))
			{
				item.FileName = MakeValidFileName(item.Name);
				if (!File.Exists(SettingsWindow.SONGS_FOLDER + "/" + item.FileName + ".mp3"))
				{
					YoutubeLink youtubeLink = YoutubeSearcher.GetYoutubeLinks(item.Name).FirstOrDefault();

					item.DownloadProgress = 10;

					if (youtubeLink == null)
					{
						item.SetDownloadStatus(false);
					}
					else
					{
						Process youtubeDownloadProcess = new Process
						{
							StartInfo =
							{
								FileName = "youtube-dl.exe",
								Arguments = string.Format(" --ffmpeg-location ./ffmpeg --extract-audio --audio-format mp3 -o \"{2}\\{0}.%(ext)s\" {1}", item.FileName, youtubeLink.Url, SettingsWindow.SONGS_FOLDER),
								CreateNoWindow = !_isDebugMode,
								WindowStyle = _isDebugMode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
								RedirectStandardOutput = true,
								UseShellExecute = false
							}
						};

						youtubeDownloadProcess.Start();

						//extract progress from process
						using (StreamReader reader = youtubeDownloadProcess.StandardOutput)
						{
							while (!youtubeDownloadProcess.HasExited)
							{
								string consoleLine = reader.ReadLine();
								if (!string.IsNullOrWhiteSpace(consoleLine))
								{
									Match match = _extractDownloadProgress.Match(consoleLine);
									if (match.Length > 0 && match.Groups.Count >= 2)
									{
										double downloadProgress;
										if (double.TryParse(match.Groups[1].Value, out downloadProgress))
										{
											item.DownloadProgress = (int)(10 + downloadProgress / 100 * 90);
										}
									}
								}
								if (CancellationPending)
								{
									youtubeDownloadProcess.Close();
									po.CancellationToken.ThrowIfCancellationRequested();
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

		private static string MakeValidFileName(string name)
		{
            return Regex.Replace(
                name,
                "[\\W-]+",  /*Matches any nonword character. Equivalent to '[^A-Za-z0-9_]'*/
                "-",
                RegexOptions.IgnoreCase);
        }
	}
}
