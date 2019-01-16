﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PlaylistDownloader.Annotations;
using System;
using System.Configuration;

//TODO 070 wait for cancel to complete before re-enabling download button

namespace PlaylistDownloader
{
    public partial class DownloadWindow : INotifyPropertyChanged
    {
        //TODO show different icon for back button than for abort button

        //TODO add max duration setting for songs
        //TODO show more detailed progress for download and conversion by using process output
        //TODO add setting to change download folder
        //TODO add setting for number of parallel processes

        private int _progressValue;
        private bool _isIndeterminate;
        private Downloader _downloader;
        private string _abortButtonLabel;
        private RunSettings _runSettings;
        private SettingsWindow _settingsWindow;

        private const string ABORT_LABEL = "Abort";
        private const string BACK_LABEL = "Back";

        public DownloadWindow(RunSettings runSettings, List<PlaylistItem> playlistItems, SettingsWindow settingsWindow)
        {
            InitializeComponent();

            _runSettings = runSettings;
            _settingsWindow = settingsWindow;
            DataContext = this;
            
            AbortButtonLabel = ABORT_LABEL;
            IsIndeterminate = false;

            PlayListItems = new ObservableCollection<PlaylistItem>();
            foreach (PlaylistItem item in playlistItems)
            {
                PlayListItems.Add(item);
            }

            StartDownload();
        }

        public void StartDownload()
        {

            _downloader = new Downloader(_runSettings, PlayListItems)
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _downloader.ProgressChanged += DownloaderProgressChanged;
            _downloader.RunWorkerCompleted += DownloaderRunWorkerCompleted;

            _downloader.RunWorkerAsync();
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

        public ObservableCollection<PlaylistItem> PlayListItems { get; }

        public PlaylistItem SelectedPlaylistItem { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AbortButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void DownloaderRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AbortButtonLabel = BACK_LABEL;
        }

        private void DownloaderProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            IsIndeterminate = false;
            ProgressValue = e.ProgressPercentage;
        }

        private void ButtonOpenFolderClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_runSettings.SongsFolder))
            {
                Directory.CreateDirectory(_runSettings.SongsFolder);
            }
            Process.Start(_runSettings.SongsFolder);
        }

        private void PlaylistItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedPlaylistItem != null &&
                SelectedPlaylistItem.DownloadProgress == 100)
            {
                string filePath = Path.GetFullPath(Path.Combine(_runSettings.SongsFolder, SelectedPlaylistItem.FileName + ".mp3"));
                if (File.Exists(filePath))
                {
                    Process.Start(filePath);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _downloader.Abort();
            _settingsWindow.Show();

            base.OnClosing(e);
        }
    }
}
