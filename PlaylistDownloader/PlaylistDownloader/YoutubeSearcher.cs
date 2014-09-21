using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace PlaylistDownloader
{
	public static class YoutubeSearcher
	{
		const string URL = "http://www.youtube.com/results?search_query={0}&page={1}";

		public static IEnumerable<YoutubeLink> GetYoutubeLinks(string query, int numberOfResults = 1)
		{
			List<YoutubeLink> links = new List<YoutubeLink>();
			int page = 1;
			while (page < 20 && links.Count() < numberOfResults)
			{
				string requestUrl = string.Format(URL, HttpUtility.UrlEncode(query).Replace("%20", "+"), page);
				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(GetWebPageCode(requestUrl));
				IEnumerable<HtmlNode> nodes = doc.DocumentNode.QuerySelectorAll("#results h3 > a");

				links.AddRange(nodes.Select(n => new YoutubeLink{ Url= "http://www.youtube.com" + n.Attributes["href"].Value, Label =  n.Attributes["title"].Value}));
				page++;
			}
			return links;
			//TODO 040 make sure program correctly stops if page is not existent => message to user
		}

		private static string GetWebPageCode(string url)
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

	public class YoutubeLink
	{
		private string _label;

		public string Label
		{
			get { return _label; }
			set { _label =HttpUtility.HtmlDecode(value); }
		}

		public string Url { get; set; }
	}
}