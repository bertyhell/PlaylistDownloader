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

using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using PlaylistDownloader.views;
using PlaylistDownloader.helpers;

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
        private readonly RunSettings _runSettings;


        public SettingsWindow()
        {
            PlaylistLogger.Info("App started");
            InitializeComponent();

            _runSettings = InitializeRunSettings();

            DataContext = this;

            PlayList = INSTRUCTIONS;
            IsIndeterminate = false;
            NumberOfResultsInput = Properties.Settings.Default.NumberOfResults.ToString();

            // Update youtube-dl.exe
            PlaylistLogger.Info("Updating youtube downloader");
            PlaylistLogger.Info("path: " + _runSettings.YoutubeDlPath);
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
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                    if (e.Data != null && e.Data != "") {
                        PlaylistLogger.Info(e.Data);
                    }
                };
                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                    if (e.Data != null && e.Data != "") {
                        PlaylistLogger.Error(e.Data);
                    }
                };
                process.Exited += (object sender, EventArgs e) => PlaylistLogger.Info("Update process exited. ");
                process.Start();
                process.BeginOutputReadLine();
            }
            else
            {
                PlaylistLogger.Error("Cannot find youtube-dl.exe. Expect file to be located in: " + _runSettings.YoutubeDlPath);
            }

        }

        private RunSettings InitializeRunSettings()
        {

            string applicationFolder =
                //Debugger.IsAttached
                //? ".\\"
                //: 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlaylistDownloader");

            // Find youtube-dl.exe
            string[] youtubeDownloadPaths = new string[] {
                Path.Combine(applicationFolder, "youtube-dl.exe"),
                Path.Combine(".", "youtube-dl.exe")
            };
            if (!File.Exists(Properties.Settings.Default.YoutubeDlPath))
            {
                if (File.Exists(youtubeDownloadPaths[0]))
                {
                    Properties.Settings.Default.YoutubeDlPath = youtubeDownloadPaths[0];
                }
                else if (File.Exists(youtubeDownloadPaths[1]))
                {
                    Properties.Settings.Default.YoutubeDlPath = youtubeDownloadPaths[1];
                }
                else
                {
                    // If it still doesn't find it => should never happen since either
                    // * the installer should install it in that directory
                    // * or the zip should contain it in the same folder as the playlist downloader exe
                    ChooseYoutubePathClick(null, null);
                }
            }

            // Find ffmpeg.exe
            string[] ffmpegPaths = new string[] {
                Path.Combine(applicationFolder, "ffmpeg", "ffmpeg.exe"),
                Path.Combine(".", "ffmpeg", "ffmpeg.exe")
            };
            if (!File.Exists(Properties.Settings.Default.FfmpegPath))
            {
                if (File.Exists(ffmpegPaths[0]))
                {
                    Properties.Settings.Default.FfmpegPath = ffmpegPaths[0];
                }
                else if (File.Exists(ffmpegPaths[1]))
                {
                    Properties.Settings.Default.FfmpegPath = ffmpegPaths[1];
                }
                else
                {
                    // If it still doesn't find it => should never happen since either
                    // * the installer should install it in that directory
                    // * or the zip should contain it in the same folder as the playlist downloader exe
                    ChooseFfmpegPathClick(null, null);
                }
            }

            var outputPath = Properties.Settings.Default.OutputPath;
            if (outputPath == null || outputPath == "")
            {
                outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "PlaylistDownloader");
            }

            var settings = new RunSettings
            {
                YoutubeDlPath = Properties.Settings.Default.YoutubeDlPath,
                FfmpegPath = Properties.Settings.Default.FfmpegPath,
                SongsFolder = outputPath
            };

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

        private void ShowLogView(object sender, RoutedEventArgs e) {
            new LogWindow().Show();
        }

        private void ButtonOpenFolderClick(object sender, RoutedEventArgs e) {

            if (!Directory.Exists(_runSettings.SongsFolder)) {
                Directory.CreateDirectory(_runSettings.SongsFolder);
            }
            Process.Start(_runSettings.SongsFolder);
        }

        private void ChooseYoutubePathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable|*.exe",
                Title = "Select youtube-dl.exe"
            };
            bool? userClickedOk = openFileDialog.ShowDialog();
            if (userClickedOk == true)
            {
                Properties.Settings.Default.YoutubeDlPath = openFileDialog.FileName;
                Properties.Settings.Default.Save();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Without youtube-dl.exe this application cannot function",
                                          "Error",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void ChooseFfmpegPathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable|*.exe",
                Title = "Select ffmpeg.exe"
            };
            bool? userClickedOk = openFileDialog.ShowDialog();
            if (userClickedOk == true)
            {
                Properties.Settings.Default.FfmpegPath = openFileDialog.FileName;
                Properties.Settings.Default.Save();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Without ffmpeg.exe this application cannot function",
                                          "Error",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void ChooseOutputPathClick(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select output folder",
                UseDescriptionForTitle = true,
                SelectedPath = Properties.Settings.Default.OutputPath
            };
            bool? showDialog = dialog.ShowDialog(this);
            if (showDialog != null && (bool)showDialog)
            {
                Properties.Settings.Default.OutputPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }
    }
}
