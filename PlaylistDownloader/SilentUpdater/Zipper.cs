using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace SilentUpdater
{
	public static class Zipper
	{
		//return root folder
		public static void ExtractZipFile(string archiveFilePathIn, string outFolder)
		{
			ZipFile zf = null;
			try
			{
				FileStream fs = File.OpenRead(archiveFilePathIn);
				zf = new ZipFile(fs);
				foreach (ZipEntry zipEntry in zf)
				{
					if (!zipEntry.IsFile)
					{
						continue;           // Ignore directories
					}
					String entryFileName = zipEntry.Name;
					// to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
					// Optionally match entrynames against a selection list here to skip as desired.
					// The unpacked length is available in the zipEntry.Size property.

					byte[] buffer = new byte[4096];     // 4K is optimum
					Stream zipStream = zf.GetInputStream(zipEntry);

					// Manipulate the output filename here as desired.
					String fullZipToPath = Path.Combine(outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName(fullZipToPath);
					if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);

					// Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
					// of the file, but does not waste memory.
					// The "using" will close the stream even if an exception occurs.
					using (FileStream streamWriter = File.Create(fullZipToPath))
					{
						StreamUtils.Copy(zipStream, streamWriter, buffer);
					}
				}
			}
			finally
			{
				if (zf != null)
				{
					zf.IsStreamOwner = true; // Makes close also shut the underlying stream
					zf.Close(); // Ensure we release resources
				}
			}
		}




		// Compresses the files in the nominated folder, and creates a zip file on disk named as outPathname.
		//
		public static void CompressFolder(string outFilePath, string folderPath)
		{

			FileStream fsOut = File.Create(outFilePath);
			ZipOutputStream zipStream = new ZipOutputStream(fsOut);

			zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

			// This setting will strip the leading part of the folder path in the entries, to
			// make the entries relative to the starting folder.
			// To include the full path for each entry up to the drive root, assign folderOffset = 0.
			int folderOffset = (folderPath).Length + (folderPath.EndsWith("\\") ? 0 : 1);

			CompressFolder(folderPath, zipStream, folderOffset);

			zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
			zipStream.Close();
		}

		// Recurses down the folder structure
		//
		private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
		{

			string[] files = Directory.GetFiles(path);

			foreach (string filename in files)
			{

				FileInfo fi = new FileInfo(filename);

				string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
				entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
				ZipEntry newEntry = new ZipEntry(entryName);
				newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

				// To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
				// you need to do one of the following: Specify UseZip64.Off, or set the Size.
				// If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
				// but the zip will be in Zip64 format which not all utilities can understand.
				zipStream.UseZip64 = UseZip64.Off;
				newEntry.Size = fi.Length;

				zipStream.PutNextEntry(newEntry);

				// Zip the file in buffered chunks
				// the "using" will close the stream even if an exception occurs
				byte[] buffer = new byte[4096];
				using (FileStream streamReader = File.OpenRead(filename))
				{
					StreamUtils.Copy(streamReader, zipStream, buffer);
				}
				zipStream.CloseEntry();
			}
			string[] folders = Directory.GetDirectories(path);
			foreach (string folder in folders)
			{
				CompressFolder(folder, zipStream, folderOffset);
			}
		}
	}
}