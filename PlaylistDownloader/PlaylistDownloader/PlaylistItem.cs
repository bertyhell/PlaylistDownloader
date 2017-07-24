using System.ComponentModel;
using System.Windows.Media;
using PlaylistDownloader.Annotations;

namespace PlaylistDownloader
{
	public class PlaylistItem : INotifyPropertyChanged
	{
		private readonly SettingsWindow _settingsWindow;
		private string _name;
		private int _downloadProgress;
		private SolidColorBrush _downloadStatusColor;

		public PlaylistItem(SettingsWindow settingsWindow)
		{
			_settingsWindow = settingsWindow;
			DownloadProgress = 0;
            Duration = 0;
			DownloadStatusColor = new SolidColorBrush(Colors.LightGreen);
		}

		public string Name
		{
			get { return _name; }
			set { _name = value.Replace("\"","").Trim(); }
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

        public double Duration { get; set; }

        public SolidColorBrush DownloadStatusColor
		{
			get { return _downloadStatusColor; }
			set
			{
				if (value.Equals(_downloadStatusColor)) return;
				_downloadStatusColor = value;
				OnPropertyChanged("DownloadStatusColor");
			}
		}

		public void SetDownloadStatus(bool success)
		{
			_settingsWindow.Dispatcher.Invoke(new SetPlaylistItemStatusDelegate(SetPlaylistStatus), this, true, success);
		}

		private delegate void SetPlaylistItemStatusDelegate(PlaylistItem playlistItem, bool isDownloadStatus, bool success);

		private static void SetPlaylistStatus(PlaylistItem playlistItem, bool isDownloadStatus, bool success)
		{
			SolidColorBrush color = new SolidColorBrush(success ? Colors.LightGreen : Colors.Red);
    		playlistItem.DownloadStatusColor = color;
		}

        public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
		    handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}