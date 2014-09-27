using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
		private const int MAX_TRIES = 125;//5 seconds

		//[download]   0.9% of 3.45MiB at 553.57KiB/s ETA 00:06
		private readonly Regex _extractDownloadProgress = new Regex(@"\[download\][\s]*([0-9\.]+)%");
		//Duration: 02:01:00.93,
		private readonly Regex _extractConversionDuration = new Regex(@"Duration:[\s]*([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2}),");
		//size=     248kB time=00:00:15.85 bitrate= 128.4kbits/s
		private readonly Regex _extractConversionProgress = new Regex(@"size=[\s0-9]+kB time=([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2})");
		private int _progress;
		private readonly int _totalSongs;
		private readonly CancellationTokenSource _cts;

		private const string TIME_SPAN_FORMAT = @"hh\:mm\:ss\.ff";

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
				//execute for both processes
				Parallel.ForEach(_playlist, po, item =>
					{
						try
						{
							DownloadPlaylistItem(item, po);
							ConvertPlaylistItem(item, po);
						}
						catch (InvalidOperationException) { } //ignore exceptions when aborting download
						catch (Win32Exception) { } //ignore process exception if killed during process exiting
					});
			}
			catch (OperationCanceledException) { } //ignore exceptions caused by canceling paralel.foreach loop
		}

		private void DownloadPlaylistItem(PlaylistItem item, ParallelOptions po)
		{
			item.DownloadProgress = 5;
			if (!string.IsNullOrWhiteSpace(item.Name))
			{
				item.FileName = MakeValidFileName(item.Name);
				if (!File.Exists("./songs/" + item.FileName + ".m4a") &&
					!File.Exists("./songs/" + item.FileName + ".mp3"))
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
								Arguments =
									string.Format(
										" --extract-audio --audio-format mp3 -o \"./songs/{0}.%(ext)s\" {1}",
										item.FileName, youtubeLink.Url),
								CreateNoWindow = true,
								WindowStyle = ProcessWindowStyle.Hidden,
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
			OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / (_totalSongs * 2), null));
		}

		private void ConvertPlaylistItem(PlaylistItem item, ParallelOptions po)
		{
			//convert m4a files to mp3
			string filePathWithoutExtension = Path.GetFullPath("./songs/" + item.FileName);
			// ReSharper disable once InconsistentNaming
			string m4aFilePath = filePathWithoutExtension + ".m4a";
			string mp3FilePath = filePathWithoutExtension + ".mp3";

			item.ConvertProgress = 5;

			if (!File.Exists(m4aFilePath) &&
				!File.Exists(mp3FilePath))
			{
				item.SetConvertStatus(false);
			}
			else
			{

				if (File.Exists(m4aFilePath) &&
					!File.Exists(mp3FilePath))
				{
					Process convertAudioProcess = null;
					try
					{
						convertAudioProcess = new Process
													  {
														  StartInfo =
														  {
															  FileName = "ffmpeg.exe",
															  Arguments =
																  string.Format("-i \"{0}\" \"{1}\"",
																	  m4aFilePath,
																	  mp3FilePath),
															  CreateNoWindow = true,
															  WindowStyle = ProcessWindowStyle.Hidden,
															  UseShellExecute = false,
															  RedirectStandardError = true,
															  RedirectStandardInput = true
														  }
													  };
						convertAudioProcess.Start();


						//extract progress from process
						using (StreamReader reader = convertAudioProcess.StandardError)
						{
							bool durationIsKnown = false;
							TimeSpan conversionDuration = new TimeSpan();
							while (!convertAudioProcess.HasExited)
							{
								//attempt to match progress
								string consoleLine = reader.ReadLine();
								if (!string.IsNullOrWhiteSpace(consoleLine))
								{
									Match match = _extractConversionProgress.Match(consoleLine);
									if (match.Length > 0 && match.Groups.Count >= 2)
									{
										if (durationIsKnown)
										{
											TimeSpan conversionProgress = TimeSpan.ParseExact(match.Groups[1].Value, TIME_SPAN_FORMAT,
												CultureInfo.InvariantCulture);
											item.ConvertProgress = (int)(5 + conversionProgress.TotalSeconds * 95 / conversionDuration.TotalSeconds);
										}
									}
									else
									{
										//attempt to match duration
										match = _extractConversionDuration.Match(consoleLine);
										if (match.Length > 0 && match.Groups.Count >= 2)
										{
											conversionDuration = TimeSpan.ParseExact(match.Groups[1].Value, TIME_SPAN_FORMAT,
												CultureInfo.InvariantCulture);
											durationIsKnown = true;
										}
									}
								}
								if (CancellationPending)
								{
									convertAudioProcess.StandardInput.WriteLine("q");
									po.CancellationToken.ThrowIfCancellationRequested();
								}
							}
						}

						Thread.Sleep(50);
					}
					finally
					{
						if (convertAudioProcess != null)
							convertAudioProcess.StandardInput.WriteLine("q");
					}
				}

				item.ConvertProgress = 90;

				if (File.Exists(m4aFilePath) &&
					File.Exists(mp3FilePath))
				{
					//delete m4a files
					int numberOfTries = 0;
					while (numberOfTries < MAX_TRIES && File.Exists(m4aFilePath))
					{
						try
						{
							File.Delete(m4aFilePath);
						}
						catch (IOException)
						{
							Thread.Sleep(40);
						}
						numberOfTries++;
					}
				}
			}

			item.ConvertProgress = 100;
			_progress++;
			OnProgressChanged(new ProgressChangedEventArgs(_progress * 100 / (_totalSongs * 2), null));
		}

		private static string MakeValidFileName(string name)
		{
			string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return Regex.Replace(name, invalidRegStr, "_");
		}
	}
}
