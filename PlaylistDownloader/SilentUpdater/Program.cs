using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using SilentUpdater.Properties;

namespace SilentUpdater
{
	class Program
	{
		private static readonly Regex VERSION_DIRECTORY_REGEX = new Regex(@"[0-9]+(\.[0-9]*)?");
		private static string _newVersionFilePath;
		private static Manifest _manifest;

		static void Main()
		{
			try
			{
				//start latest local version
				string currentVersionExe =
					Path.GetFullPath(Path.Combine(Settings.Default.CurrentVersion, Settings.Default.ExecutablePath));
				if (File.Exists(currentVersionExe))
					Process.Start(currentVersionExe);

				//check online if new version is availible
				_manifest = GetManifest();

				if (double.Parse(_manifest.LatestVersion) > double.Parse(Settings.Default.CurrentVersion))
				{
					//update is needed

					//download new version zip
					_newVersionFilePath = Path.GetFullPath(_manifest.LatestVersion + ".zip");
					DownloadNewVersion(_manifest.DistUrl, _newVersionFilePath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private static void DownloadNewVersion(string url, string filePath)
		{
			new WebClient().DownloadFile(new Uri(url), filePath);

			//unzip new version
			Zipper.ExtractZipFile(_newVersionFilePath, Path.GetFullPath(_manifest.LatestVersion));
			string lastVersion = Settings.Default.CurrentVersion;
			Settings.Default.CurrentVersion = _manifest.LatestVersion;
			Settings.Default.Save();
			File.Delete(_newVersionFilePath);

			//remove older versions
			List<string> versionDirs = Directory.GetDirectories(".")
				.Select(Path.GetFileName)
				.Where(d => VERSION_DIRECTORY_REGEX.IsMatch(d))
				.OrderByDescending(double.Parse)
				.Except(new[]{lastVersion, Settings.Default.CurrentVersion})
				.ToList();

			foreach (string versionDir in versionDirs)
			{
				try
				{
					Directory.Delete(versionDir);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}

		private static Manifest GetManifest()
		{
			string manifestJson = GetWebPageCode(Settings.Default.UpdateManifestPath);
			byte[] byteArray = Encoding.UTF8.GetBytes(manifestJson);
			MemoryStream stream = new MemoryStream(byteArray);

			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Manifest));
			return (Manifest)serializer.ReadObject(stream);
		}

		private static string GetWebPageCode(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				Stream receiveStream = response.GetResponseStream();
				StreamReader readStream;
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
