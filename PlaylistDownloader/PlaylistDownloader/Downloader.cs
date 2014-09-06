using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace PlaylistDownloader
{
	public class Downloader : BackgroundWorker
	{
		const string URL = "http://www.youtube.com/results?search_query={0}";

		private readonly ICollection<PlaylistItem> _playlist;
		private const int MAX_TRIES = 125;//5 seconds

		public Downloader(ICollection<PlaylistItem> playlist)
		{
			_playlist = playlist;
		}

		protected override void OnDoWork(DoWorkEventArgs e)
		{
			int totalSongs = _playlist.Count;
			int progress = 0;

			Parallel.ForEach(_playlist, item =>
				{
					if (!string.IsNullOrWhiteSpace(item.Name))
					{
						item.FileName = MakeValidFileName(item.Name);
						if (!File.Exists("./songs/" + item.FileName + ".m4a") &&
							!File.Exists("./songs/" + item.FileName + ".mp3"))
						{
							string requestUrl = string.Format(URL, HttpUtility.UrlEncode(item.Name).Replace("%20", "+"));

							string youtubeLink = GetYoutubeLinks(GetWebPageCode(requestUrl)).FirstOrDefault();
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
												item.FileName, youtubeLink),
										CreateNoWindow = true,
										WindowStyle = ProcessWindowStyle.Hidden
									}
								};
								youtubeDownloadProcess.Start();
								youtubeDownloadProcess.WaitForExit();
								Thread.Sleep(1000);
							}

						}
					}

					item.DownloadProgress = 100;
					progress++;
					OnProgressChanged(new ProgressChangedEventArgs(progress * 100 / (totalSongs * 2), null));

					if (CancellationPending)
					{
						return;
					}
					//convert m4a files to mp3
					string filePathWithoutExtension = Path.GetFullPath("./songs/" + item.FileName);
					// ReSharper disable once InconsistentNaming
					string m4aFilePath = filePathWithoutExtension + ".m4a";
					string mp3FilePath = filePathWithoutExtension + ".mp3";

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
							Process convertAudioProcess = new Process
														  {
															  StartInfo =
															  {
																  FileName = "ffmpeg.exe",
																  Arguments =
																	  string.Format("-i \"{0}\" \"{1}\"",
																		  m4aFilePath,
																		  mp3FilePath),
																  CreateNoWindow = true,
																  WindowStyle = ProcessWindowStyle.Hidden
															  }
														  };
							convertAudioProcess.Start();
							convertAudioProcess.WaitForExit();

							Thread.Sleep(50);
						}

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
					progress++;
					OnProgressChanged(new ProgressChangedEventArgs(progress * 100 / (totalSongs * 2), null));
				});
		}

		private IEnumerable<string> GetYoutubeLinks(string htmlCode)
		{
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(htmlCode);
			IEnumerable<HtmlNode> nodes = doc.DocumentNode.QuerySelectorAll("#results h3 > a");
			return nodes.Select(n => "http://www.youtube.com" + n.Attributes["href"].Value).ToArray();
		}

		private string GetWebPageCode(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				Stream receiveStream = response.GetResponseStream();
				StreamReader readStream = null;
				if (response.CharacterSet == null)
				{
					readStream = new StreamReader(receiveStream);
				}
				else
				{
					readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
				}
				string data = readStream.ReadToEnd();
				response.Close();
				readStream.Close();
				return data;
			}
			return null;
		}
		private static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
		}
	}
}
