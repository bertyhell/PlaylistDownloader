using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PlaylistDownloader.Annotations;

namespace PlaylistDownloader
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private int _progressValue;
		private string _playList;
		private bool _isIndeterminate;
		private const string INSTRUCTIONS = "Enter songs (one per line)";

		public MainWindow()
		{
			InitializeComponent();

			DataContext = this;

			PlayList = INSTRUCTIONS;
			IsIndeterminate = false;
		}

		public string PlayList
		{
			get { return _playList; }
			set
			{
				if (value == _playList) return;
				_playList = value;
				OnPropertyChanged("PlayList");
			}
		}

		public int ProgressValue
		{
			get { return _progressValue; }
			set
			{
				if (value == _progressValue) return;
				_progressValue = value;
				OnPropertyChanged("ProgressValue");
			}
		}

		public bool IsIndeterminate
		{
			get { return _isIndeterminate; }
			set
			{
				if (value.Equals(_isIndeterminate)) return;
				_isIndeterminate = value;
				OnPropertyChanged("IsIndeterminate");
			}
		}

		private void DownloadButtonClick(object sender, RoutedEventArgs e)
		{
			IsIndeterminate = true;
			List<string> playlistSongs = PlayList.Split('\n').Select(s => s.Trim()).ToList();
			Downloader downloader = new Downloader(playlistSongs)
			                        {
				                        WorkerReportsProgress = true,
				                        WorkerSupportsCancellation = true
			                        };

			downloader.ProgressChanged += DownloaderProgressChanged;

			downloader.RunWorkerAsync();
		}

		private void DownloaderProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			IsIndeterminate = false;
			ProgressValue = e.ProgressPercentage;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		private void TextBoxMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (PlayList.Equals(INSTRUCTIONS)) PlayList = "";
		}
	}
}
