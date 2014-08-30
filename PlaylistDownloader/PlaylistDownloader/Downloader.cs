using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace PlaylistDownloader
{
	public class Downloader : BackgroundWorker
	{
		const string URL = "http://www.youtube.com/results?search_query={0}";

		private readonly ICollection<PlaylistItem> _playlist;

		public Downloader(ICollection<PlaylistItem> playlist)
		{
			_playlist = playlist;
		}

		protected override void OnDoWork(DoWorkEventArgs e)
		{
			int totalSongs = _playlist.Count;
			int progress = 0;

			foreach (PlaylistItem item in _playlist)
			{
				if (!string.IsNullOrWhiteSpace(item.Name))
				{
					item.FileName = MakeValidFileName(item.Name);
					if (!File.Exists("./songs/" + item.FileName + ".m4a") && !File.Exists("./songs/" + item.FileName + ".mp3"))
					{
						string requestUrl = string.Format(URL, System.Uri.EscapeDataString(item.Name.Replace(" ", "+")));

						string youtubeLink = GetYoutubeLinks(GetWebPageCode(requestUrl))[0];

						Process youtubeDownloadProcess = new Process
								 {
									 StartInfo =
									 {
										 FileName = "youtube-dl.exe",
										 Arguments =
											 string.Format(" --extract-audio --audio-format mp3 -o \"./songs/{0}.%(ext)s\" {1}", item.FileName, youtubeLink),
										 CreateNoWindow = true,
										 WindowStyle = ProcessWindowStyle.Hidden
									 }
								 };
						youtubeDownloadProcess.Start();
						youtubeDownloadProcess.WaitForExit();
					}
				}

				item.DownloadProgress = 100;
				progress++;
				OnProgressChanged(new ProgressChangedEventArgs((progress) * 50 / totalSongs, null));

				if (CancellationPending)
				{
					return;
				}
			}

			//convert m4a files to mp3
			progress = 0;
			foreach (PlaylistItem item in _playlist)
			{
				string filePathWithoutExtension = Path.GetFullPath("./songs/" + item.FileName);
				if (File.Exists(filePathWithoutExtension + ".m4a") && !File.Exists(filePathWithoutExtension + ".mp3"))
				{
					Process convertAudioProcess = new Process
					{
						StartInfo =
						{
							FileName = "ffmpeg.exe",
							Arguments = string.Format("-i \"{0}\" \"{1}\"", filePathWithoutExtension + ".m4a", filePathWithoutExtension + ".mp3"),
							CreateNoWindow = true,
							WindowStyle = ProcessWindowStyle.Hidden
						}
					};
					convertAudioProcess.Start();
					convertAudioProcess.WaitForExit();

					Thread.Sleep(50);
				}

				if (File.Exists(filePathWithoutExtension + ".m4a") && File.Exists(filePathWithoutExtension + ".mp3"))
				{
					//delete m4a files
					File.Delete(filePathWithoutExtension + ".m4a");
				}

				item.ConvertProgress = 100;
				progress++;
				OnProgressChanged(new ProgressChangedEventArgs(50 + progress * 50 / totalSongs, null));

				if (CancellationPending)
				{
					return;
				}
			}
		}

		private string[] GetYoutubeLinks(string htmlCode)
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
