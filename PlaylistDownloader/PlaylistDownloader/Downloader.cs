using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace PlaylistDownloader
{
	public class Downloader : BackgroundWorker
	{
		const string URL = "http://www.youtube.com/results?search_query={0}";

		private readonly List<string> _playlist;

		public Downloader(List<string> playlist)
		{
			_playlist = playlist;
		}

		protected override void OnDoWork(DoWorkEventArgs e)
		{
			int totalSongs = _playlist.Count;
			for (int i = 0; i < _playlist.Count; i++)
			{
				string song = _playlist[i];
				string requestUrl = string.Format(URL, System.Uri.EscapeDataString(song.Replace(" ", "+")));

				string youtubeLink = GetYoutubeLinks(GetWebPageCode(requestUrl))[0];

				Process youtubeDownloadProcess = new Process
												 {
													 StartInfo =
													 {
														 FileName = "youtube-dl.exe",
														 Arguments = " --extract-audio --audio-format mp3 -o ./songs/%(title)s.%(ext)s " + youtubeLink,
														 CreateNoWindow = true,
														 WindowStyle = ProcessWindowStyle.Hidden
													 }
												 };
				youtubeDownloadProcess.Start();
				youtubeDownloadProcess.WaitForExit();

				OnProgressChanged(new ProgressChangedEventArgs((i + 1) * 50 / totalSongs, null));

				if (CancellationPending)
				{
					return;
				}
			}

			//convert m4a files to mp3
			string[] musicFiles = Directory.GetFiles("./songs", "*.m4a");
			for (int i = 0; i < musicFiles.Length; i++)
			{
				string file = musicFiles[i];
				Process convertAudioProcess = new Process
				{
					StartInfo =
					{
						FileName = "ffmpeg.exe",
						Arguments = string.Format("-i \"{0}\" \"{1}\"", file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".mp3")),
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden
					}
				};
				convertAudioProcess.Start();
				convertAudioProcess.WaitForExit();

				//delete m4a files
				File.Delete(file);

				OnProgressChanged(new ProgressChangedEventArgs(50 + (i + 1) * 50 / totalSongs, null));
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
	}
}
