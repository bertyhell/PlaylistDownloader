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
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace PlaylistDownloader
{
    public partial class SettingsWindow : INotifyPropertyChanged
    {
        //TODO show different icon for back button than for abort button

        //TODO add max duration setting for songs
        //TODO show more detailed progress for download and conversion by using process output
        //TODO add setting to change download folder
        //TODO if youtube-dl.exe file can't be found in the programdata folder try to find it in the current folder
        //TODO make sure all non alpha numeric chars are removed
        //TODO make sure if name cleanup results in no alpha numeric chars that there is a default name that doesn't collide with other empty names

        private string _playList;
        private bool _isIndeterminate;
        private string _abortButtonLabel;
        private string _query;
        private string _numberOfResultsInput;
        private int _numberOfResults;
        private bool _isNumberOfResultsValid;
        private bool _isQueryValid;
        private const string INSTRUCTIONS = "Enter songs (one per line)";
        private readonly bool _isDebugMode = bool.Parse(ConfigurationManager.AppSettings.Get("debug"));
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly RunSettings _runSettings;


        public SettingsWindow()
        {
            Logger.Info("App started");
            InitializeComponent();

            _runSettings = InitializeRunSettings();

            DataContext = this;

            PlayList = INSTRUCTIONS;
            IsIndeterminate = false;
            NumberOfResultsInput = Properties.Settings.Default.NumberOfResults.ToString();

            // Update youtube-dl.exe
            Logger.Info("Updating youtube downloader");
            if (File.Exists(_runSettings.YoutubeDlPath))
            {
                Process process = new Process
                {
                    StartInfo =
                    {
                        FileName = _runSettings.YoutubeDlPath,
                        Arguments = " -U",
                        CreateNoWindow = true,
                        WindowStyle = _isDebugMode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => Logger.Info(e.Data);
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => Logger.Error(e.Data);
                process.Exited += (object sender, EventArgs e) => Logger.Info("Update process existed. ");
                process.Start();
                process.BeginOutputReadLine();
            }
            else
            {
                Logger.Error("Cannot find youtube-dl.exe. Expect file to be located in: " + _runSettings.YoutubeDlPath);
            }

        }

        private RunSettings InitializeRunSettings()
        {
            string applicationFolder = Debugger.IsAttached
                ? ".\\"
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PlaylistDownloader");

            if (!Directory.Exists(applicationFolder))
            {
                applicationFolder = ".\\";
            }

            if (!File.Exists(Properties.Settings.Default.YoutubeDlPath)) {
                // set default path
                Properties.Settings.Default.YoutubeDlPath = Path.Combine(applicationFolder, "youtube-dl.exe");
            }

            ChooseYoutubePath_Click(null, null);

            if (!File.Exists(Properties.Settings.Default.FfmpegPath))
            {
                // set default path
                Properties.Settings.Default.FfmpegPath = Path.Combine(applicationFolder, "ffmpeg", "ffmpeg.exe");
            }

            ChooseFfmpegPath_Click(null, null);

            if (!Directory.Exists(Properties.Settings.Default.OutputPath))
            {
                // set default path
                Properties.Settings.Default.OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "PlaylistDownloader");
            }

            

            var settings = new RunSettings
            {
                YoutubeDlPath = Properties.Settings.Default.YoutubeDlPath,
                FfmpegPath = Properties.Settings.Default.FfmpegPath,
                SongsFolder = Properties.Settings.Default.OutputPath
            };

            //  TODO 005: Get all settings from the configuration file

            settings.IsDebug = _isDebugMode;
            if (_isDebugMode)
            {
                settings.NormalizedSuffix = "-normalized";
            }

            return settings;
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
                if (IsNumberOfResultsValid)
                {
                    Properties.Settings.Default.NumberOfResults = _numberOfResults;
                    Properties.Settings.Default.Save();
                }

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
            get { return !string.IsNullOrWhiteSpace(PlayList) && !PlayList.Equals(INSTRUCTIONS); }
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
            IsIndeterminate = true;

            List<PlaylistItem> playlistItems = new List<PlaylistItem>();
            PlayList
                .Split('\n')
                .Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
                .ToList().
                ForEach(s => playlistItems.Add(new PlaylistItem(this) { Name = s }));

            new DownloadWindow(_runSettings, playlistItems, this).Show();
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
            Directory.CreateDirectory(_runSettings.SongsFolder);
            Process.Start(_runSettings.SongsFolder);
        }

        private void ChooseYoutubePath_Click(object sender, RoutedEventArgs e)
        {
            var forceChooseDialog = false;
            if (sender != null)
            {
                // user clicked choose button
                forceChooseDialog = true;
            }

            bool changed = false;

            while (!File.Exists(Properties.Settings.Default.YoutubeDlPath) || forceChooseDialog)
            {
                forceChooseDialog = false;
                var ret = Microsoft.VisualBasic.Interaction.InputBox(
                    "Please enter full path to youtube-dl.exe", "Cannot find Youtube-dl", Properties.Settings.Default.YoutubeDlPath);
                if (string.IsNullOrEmpty(ret))
                {
                    return;
                }
                else
                {
                    Properties.Settings.Default.YoutubeDlPath = ret;
                    changed = true;
                }
            }

            if (changed)
            {
                Properties.Settings.Default.Save();
            }
        }

        private void ChooseFfmpegPath_Click(object sender, RoutedEventArgs e)
        {
            var forceChooseDialog = false;
            if (sender != null)
            {
                // user clicked choose button
                forceChooseDialog = true;
            }

            bool changed = false;

            while (!File.Exists(Properties.Settings.Default.FfmpegPath) || forceChooseDialog)
            {
                forceChooseDialog = false;
                var ret = Microsoft.VisualBasic.Interaction.InputBox(
                    "Please enter full path to ffmpeg.exe", "Cannot find ffmpeg", Properties.Settings.Default.FfmpegPath);
                if (string.IsNullOrEmpty(ret))
                {
                    return;
                }
                else
                {
                    Properties.Settings.Default.FfmpegPath = ret;
                    changed = true;
                }
            }

            if (changed)
            {
                Properties.Settings.Default.Save();
            }
        }

        private void ChooseOutputPathClick(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select output folder",
                UseDescriptionForTitle = true
            };
            bool? showDialog = dialog.ShowDialog(this);
            if (showDialog != null && (bool)showDialog)
            {
                Properties.Settings.Default.OutputPath = dialog.SelectedPath;
                Directory.CreateDirectory(Properties.Settings.Default.OutputPath);
                Properties.Settings.Default.Save();
            }
        }
    }
}
