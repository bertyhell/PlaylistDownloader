using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PlaylistDownloader.Annotations;
using System;
using System.Configuration;
using NLog;

namespace PlaylistDownloader
{
	public partial class SettingsWindow : INotifyPropertyChanged
	{
		//TODO show different icon for back button than for abort button

		//TODO add max duration setting for songs
		//TODO show more detailed progress for download and conversion by using process output
		//TODO for exit process on abort or close
		//TODO add setting to change download folder
		//TODO add setting for number of parallel processes
        //TODO switch mymusic folder to the downloads folder
        
		private string _playList;
		private bool _isIndeterminate;
		//private Downloader _downloader;
		private bool _isEditPanelVisible;
		private string _abortButtonLabel;
		private string _query;
		private string _numberOfResultsInput;
		private int _numberOfResults;
		private bool _isNumberOfResultsValid;
		private bool _isQueryValid;
		private const string INSTRUCTIONS = "Enter songs (one per line)";
        public static readonly string SONGS_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "PlaylistDownloader");
        private readonly bool _isDebugMode = bool.Parse(ConfigurationManager.AppSettings.Get("debug"));
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SettingsWindow()
		{
            Logger.Info("App started");
			InitializeComponent();

			DataContext = this;

			PlayList = INSTRUCTIONS;
			IsIndeterminate = false;
			IsEditPanelVisible = true;
			NumberOfResultsInput = "20";

            // Update youtube-dl.exe
            Logger.Info("Updating youtube downloader");
            var youtubeDlExePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "PlaylistDownloader", "youtube-dl.exe");
            if (File.Exists(youtubeDlExePath))
            {
                Process process = new Process
                {
                    StartInfo =
                    {
                        FileName = youtubeDlExePath,
                        Arguments = " -U",
                        CreateNoWindow = true,
                        WindowStyle = _isDebugMode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;

                //Logger.Info(process.StandardOutput.ReadToEndAsync());
                //Logger.Error(process.StandardError.ReadToEndAsync());
                process.Exited += Process_Exited;
                process.Start();
                process.BeginOutputReadLine();
            }
            else
            {
                Logger.Error("Cannot find youtube-dl.exe. Expect file to be located in: " + youtubeDlExePath);
            }

		}

        private void Process_Exited(object sender, EventArgs e)
        {
            Logger.Info("Update process existed. ");
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Error(e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info(e.Data);
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
			IsEditPanelVisible = false;
			IsIndeterminate = true;
            
            List<PlaylistItem> playlistItems = new List<PlaylistItem>();
			PlayList
				.Split('\n')
				.Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
				.ToList().
				ForEach(s => playlistItems.Add(new PlaylistItem(this) { Name = s }));

		    new DownloadWindow(playlistItems, this).Show();
            Hide();
		}
        
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
		    handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void TextBoxMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (PlayList.Equals(INSTRUCTIONS)) PlayList = "";
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
			Directory.CreateDirectory(SONGS_FOLDER);
			Process.Start(SONGS_FOLDER);
		}
        
	}
}
