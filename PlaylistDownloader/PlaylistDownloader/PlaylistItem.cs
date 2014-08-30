using System.ComponentModel;
using PlaylistDownloader.Annotations;

namespace PlaylistDownloader
{
	public class PlaylistItem : INotifyPropertyChanged
	{
		private string _name;
		private int _convertProgress;
		private int _downloadProgress;

		public PlaylistItem()
		{
			DownloadProgress = 0;
			ConvertProgress = 0;
		}

		public string Name
		{
			get { return _name; }
			set { _name = value.Trim(); }
		}

		public string FileName { get; set; }

		public int DownloadProgress
		{
			get { return _downloadProgress; }
			set
			{
				if (value == _downloadProgress) return;
				_downloadProgress = value;
				OnPropertyChanged("DownloadProgress");
			}
		}

		public int ConvertProgress
		{
			get { return _convertProgress; }
			set
			{
				if (value == _convertProgress) return;
				_convertProgress = value;
				OnPropertyChanged("ConvertProgress");
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}