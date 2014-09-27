﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PlaylistDownloader.Annotations;

namespace PlaylistDownloader
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		//TODO show different icon for back button than for abort button

		//TODO add max duration setting for songs
		//TODO show more detailed progress for download and conversion by using process output
		//TODO for exit process on abort or close
		//TODO add setting to change download folder
		//TODO add setting for number of parallel processes

		private int _progressValue;
		private string _playList;
		private bool _isIndeterminate;
		private Downloader _downloader;
		private bool _isEditPanelVisible;
		private string _abortButtonLabel;
		private string _query;
		private string _numberOfResultsInput;
		private int _numberOfResults;
		private bool _isNumberOfResultsValid;
		private bool _isQueryValid;
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
			NumberOfResultsInput = "20";

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
				OnPropertyChanged("IsDownloadButtonEnabled");
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

		public string Query
		{
			get { return _query; }
			set
			{
				if (value == _query) return;
				_query = value;
				IsQueryValid = !string.IsNullOrWhiteSpace(_query);
				OnPropertyChanged("Query");
			}
		}

		public bool IsQueryValid
		{
			get { return _isQueryValid; }
			set
			{
				_isQueryValid = value;
				OnPropertyChanged("IsSearchButtonEnabled");
				OnPropertyChanged("SearchButtonError");
			}
		}

		public string NumberOfResultsInput
		{
			get { return _numberOfResultsInput; }
			set
			{
				if (value == _numberOfResultsInput) return;
				_numberOfResultsInput = value;
				IsNumberOfResultsValid = int.TryParse(_numberOfResultsInput, out _numberOfResults);
				OnPropertyChanged("NumberOfResultsInput");
			}
		}

		public bool IsNumberOfResultsValid
		{
			get { return _isNumberOfResultsValid; }
			set
			{
				if (value.Equals(_isNumberOfResultsValid)) return;
				_isNumberOfResultsValid = value;
				OnPropertyChanged("IsSearchButtonEnabled");
				OnPropertyChanged("SearchButtonError");
			}
		}

		public bool IsDownloadButtonEnabled
		{
			get { return !PlayList.Equals(INSTRUCTIONS) && !string.IsNullOrWhiteSpace(PlayList); }
		}

		public string DownloadButtonError
		{
			get { return IsDownloadButtonEnabled ? "" : "Playlist needs to contain some song titles"; }
		}

		public bool IsSearchButtonEnabled
		{
			get { return IsNumberOfResultsValid && IsQueryValid; }
		}

		public string SearchButtonError
		{
			get { return (IsQueryValid ? "" : "The query has to be filled in") + (IsNumberOfResultsValid ? "" : "Number of results is not a valid number"); }
		}

		public PlaylistItem SelectedPlaylistItem { get; set; }

		private void DownloadButtonClick(object sender, RoutedEventArgs e)
		{
			AbortButtonLabel = ABORT_LABEL;
			IsEditPanelVisible = false;
			IsIndeterminate = true;

			PlayListItems.Clear();
			PlayList
				.Split('\n')
				.Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
				.ToList().
				ForEach(s => PlayListItems.Add(new PlaylistItem(this) { Name = s }));

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

		private void TextBoxMouseDown(object sender, MouseButtonEventArgs e)
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

		private void ButtonSearchClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Query))
			{
				MessageBox.Show("Query cannot be empty");
				return;
			}
			int numberOfResults;
			if (!int.TryParse(NumberOfResultsInput, out numberOfResults) || numberOfResults < 1)
			{
				MessageBox.Show("Number of pages is not a valid number");
			}

			IEnumerable<YoutubeLink> youtubeLinks = YoutubeSearcher.GetYoutubeLinks(Query, numberOfResults);
			if (PlayList == INSTRUCTIONS) PlayList = "";
			foreach (YoutubeLink link in youtubeLinks)
			{
				PlayList += link.Label + "\n"; //TODO store urls of songs in cache => quicker to download, no need for lookup on youtube
			}
		}

		private void ButtonOpenFolderClick(object sender, RoutedEventArgs e)
		{
			Directory.CreateDirectory("songs");
			Process.Start("songs");
		}

		private void PlaylistItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedPlaylistItem != null &&
				SelectedPlaylistItem.ConvertProgress == 100 &&
				SelectedPlaylistItem.DownloadProgress == 100)
			{
				string filePath = Path.GetFullPath("./songs/" + SelectedPlaylistItem.FileName + ".mp3");
				if (File.Exists(filePath))
				{
					Process.Start(filePath);
				}
			}
		}
	}
}
