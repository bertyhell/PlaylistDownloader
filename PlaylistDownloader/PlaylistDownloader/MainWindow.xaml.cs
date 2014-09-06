using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
		private Downloader _downloader;
		private bool _isEditPanelVisible;
		private string _abortButtonLabel;
		private const string INSTRUCTIONS = "Enter songs (one per line)";
		private const string ABORT_LABEL = "Abort";
		private const string BACK_LABEL = "Back";


		public MainWindow()
		{
			InitializeComponent();

			DataContext = this;

			PlayList = INSTRUCTIONS;
			AbortButtonLabel = ABORT_LABEL;
			IsIndeterminate = false;
			IsEditPanelVisible = true;

			PlayListItems = new ObservableCollection<PlaylistItem>();

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

		public string AbortButtonLabel
		{
			get { return _abortButtonLabel; }
			set
			{
				if (value == _abortButtonLabel) return;
				_abortButtonLabel = value;
				OnPropertyChanged("AbortButtonLabel");
			}
		}

		public ObservableCollection<PlaylistItem> PlayListItems { get; private set; }

		public Visibility ProgressPanelVisibility
		{
			get
			{
				return IsEditPanelVisible ? Visibility.Hidden : Visibility.Visible;
			}
		}

		public Visibility EditPanelVisibility
		{
			get
			{
				return IsEditPanelVisible ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public bool IsEditPanelVisible
		{
			get { return _isEditPanelVisible; }
			set
			{
				if (value.Equals(_isEditPanelVisible)) return;
				_isEditPanelVisible = value;
				OnPropertyChanged("IsEditPanelVisible");
				OnPropertyChanged("ProgressPanelVisibility");
				OnPropertyChanged("EditPanelVisibility");
			}
		}

		private void DownloadButtonClick(object sender, RoutedEventArgs e)
		{
			IsEditPanelVisible = false;
			IsIndeterminate = true;

			PlayListItems.Clear();
			PlayList.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s.Trim())).ToList().ForEach(s => PlayListItems.Add(new PlaylistItem(this) { Name = s }));

			_downloader = new Downloader(PlayListItems)
						  {
							  WorkerReportsProgress = true,
							  WorkerSupportsCancellation = true
						  };

			_downloader.ProgressChanged += DownloaderProgressChanged;
			_downloader.RunWorkerCompleted += DownloaderRunWorkerCompleted;


			_downloader.RunWorkerAsync();
		}

		void DownloaderRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//TODO 070 wait for cancel to complete before re-enabling download button
			AbortButtonLabel = BACK_LABEL;
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


		private void AbortButtonClick(object sender, RoutedEventArgs e)
		{
			if (AbortButtonLabel == ABORT_LABEL)
			{
				_downloader.CancelAsync();
				_downloader = null;
				ProgressValue = 0;
				IsEditPanelVisible = true;
			}
			else
			{
				IsEditPanelVisible = true;
				AbortButtonLabel = ABORT_LABEL;
			}
		}

		private void WindowLoaded(object sender, RoutedEventArgs e)
		{
			PlaylistTextBox.Focus();
			PlaylistTextBox.SelectAll();
		}
	}
}
