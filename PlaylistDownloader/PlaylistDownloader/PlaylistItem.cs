using System.ComponentModel;
using System.Windows.Media;
using PlaylistDownloader.Annotations;

namespace PlaylistDownloader
{
	public class PlaylistItem : INotifyPropertyChanged
	{
		private readonly MainWindow _mainWindow;
		private string _name;
		private int _convertProgress;
		private int _downloadProgress;
		private SolidColorBrush _downloadStatusColor;
		private SolidColorBrush _convertStatusColor;

		public PlaylistItem(MainWindow mainWindow)
		{
			_mainWindow = mainWindow;
			DownloadProgress = 0;
			ConvertProgress = 0;
			DownloadStatusColor = new SolidColorBrush(Colors.LightGreen);
			ConvertStatusColor = new SolidColorBrush(Colors.LightGreen);
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

		public SolidColorBrush ConvertStatusColor
		{
			get { return _convertStatusColor; }
			set
			{
				if (value.Equals(_convertStatusColor)) return;
				_convertStatusColor = value;
				OnPropertyChanged("ConvertStatusColor");
			}
		}

		public void SetDownloadStatus(bool success)
		{
			_mainWindow.Dispatcher.Invoke(new SetPlaylistItemStatusDelegate(SetPlaylistStatus), this, true, success);
		}

		public void SetConvertStatus(bool success)
		{
			_mainWindow.Dispatcher.Invoke(new SetPlaylistItemStatusDelegate(SetPlaylistStatus), this, false, success);
		}

		private delegate void SetPlaylistItemStatusDelegate(PlaylistItem playlistItem, bool isDownloadStatus, bool success);

		private static void SetPlaylistStatus(PlaylistItem playlistItem, bool isDownloadStatus, bool success)
		{
			SolidColorBrush color = new SolidColorBrush(success ? Colors.LightGreen : Colors.Red);
			if (isDownloadStatus)
			{
				playlistItem.DownloadStatusColor = color;
			}
			else
			{
				playlistItem.ConvertStatusColor = color;
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