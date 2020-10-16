using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Manatee.Json;
using Manatee.Json.Path;
using Manatee.Json.Pointer;
using MinimumEditDistance;
using NLog;
using MoreLinq;

namespace PlaylistDownloader {
    public class Person {
        public string name;
        public string email;
    }

    public static class YoutubeSearcher {
        private const string URL = "http://www.youtube.com/results?search_query={0}&page={1}";
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static IEnumerable<YoutubeLink> GetYoutubeLinks(string query, int numberOfResults = 1) {
            List<YoutubeLink> links = null;

            if (query.StartsWith("http")) {
                //  Get title
                links = GetLinksFromUrl(query);
            } else {
                int page = 1;
                while (page < 20 && (links == null || links.Count < numberOfResults)) {
                    string requestUrl = string.Format(URL, HttpUtility.UrlEncode(query)?.Replace("%20", "+"), page);

                    links = GetLinksFromUrl(query);

                    page++;
                }
            }

            List<YoutubeLink> uniqueLinks = links.DistinctBy((link) => link.Label).DistinctBy((link) => link.Url).ToList();
            Dictionary<string, int> distances = uniqueLinks.Select(link => new KeyValuePair<string, int>(link.Label, Levenshtein.CalculateDistance(link.Label, query, 1))).ToDictionary(x => x.Key, x => x.Value);
            return uniqueLinks.OrderBy(link => distances[link.Label]);

            // TODO 040 make sure program correctly stops if page is not existent => message to user
        }

        private static List<YoutubeLink> GetLinksFromUrl(string url) {
            List<YoutubeLink> links = new List<YoutubeLink>();
            string html = GetWebPageCode(url);
            string initialDataLine = html.Split('\n').FirstOrDefault((line) => line.Contains("window[\"ytInitialData\"] = ") || line.Contains("var ytInitialData = "));
            if (initialDataLine == null) {
                return links;
            }
            string jsonData = initialDataLine
                .Split(new[] { "window[\"ytInitialData\"] = " }, StringSplitOptions.None)
                .Last()
                .Split(new[] { "var ytInitialData = " }, StringSplitOptions.None)
                .Last()
                .Trim(' ')
                .Trim(';');

            JsonValue jsonObject = JsonValue.Parse(jsonData);

            if (jsonObject == null) {
                return links;
            }

            List<JsonArray> videos = new List<JsonArray>(){
                JsonPath.Parse("$..videoPrimaryInfoRenderer").Evaluate(jsonObject),
                JsonPath.Parse("$..childVideoRenderer").Evaluate(jsonObject),
                JsonPath.Parse("$..videoRenderer").Evaluate(jsonObject)
            };

            JsonPointer videoIdPointer = new JsonPointer { "videoId" };
            JsonPointer videoTitlePointer = new JsonPointer { "title", "simpleText" };
            JsonPointer videoTitlePointer2 = new JsonPointer { "title", "runs", "0", "text" };
            videos.ToList().ForEach((videoArray, videoArrayIndex) => {
                videoArray.ToList().ForEach((videoInfo) => {
                    string json = videoInfo.ToString();
                    PointerEvaluationResults videoIdResults = videoIdPointer.Evaluate(videoInfo);
                    PointerEvaluationResults videoTitleResults = videoTitlePointer.Evaluate(videoInfo);
                    PointerEvaluationResults videoTitleResults2 = videoTitlePointer2.Evaluate(videoInfo);

                    string videoId = videoIdResults?.Result?.String;
                    string videoTitle = videoTitleResults?.Result?.String ?? videoTitleResults2?.Result?.String;
                    if ((videoId != null || videoArrayIndex == 0) && videoTitle != null) {
                        string videoUrl = videoId == null ? url : ("https://www.youtube.com/watch?v=" + videoId);
                        links.Add(new YoutubeLink { Url = videoUrl, Label = videoTitle });
                    }
                });
            });

            return links;
        }

        private static string GetWebPageCode(string url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK) {
                Stream receiveStream = response.GetResponseStream();
                if (receiveStream != null) {
                    StreamReader readStream;
                    if (response.CharacterSet == null) {
                        readStream = new StreamReader(receiveStream);
                    } else {
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                    }
                    string data = readStream.ReadToEnd();
                    response.Close();
                    readStream.Close();
                    return data;
                } else {
                    logger.Warn("No response from " + url);
                }
                return null;
            }
            return null;
        }
    }

    public class YoutubeLink {
        private string _label;

        public string Label {
            get { return _label; }
            set { _label = HttpUtility.HtmlDecode(value); }
        }

        public string Url { get; set; }
    }
}