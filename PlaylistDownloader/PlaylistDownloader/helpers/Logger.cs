using NLog;
using System;
using System.Collections.ObjectModel;

namespace PlaylistDownloader.helpers {
    class PlaylistLogger {
        public static ObservableCollection<LogInfo> Logs = new ObservableCollection<LogInfo>();
        private static readonly Logger Logger = LogManager.GetLogger("global");

        public static void Error(string message) {
            App.Current.Dispatcher.Invoke(delegate {
                Logs.Add(new LogInfo(message, LogLevel.Error));
            });
            Logger.Error(message);
        }

        public static void Info(string message) {
            App.Current.Dispatcher.Invoke(delegate
            {
                Logs.Add(new LogInfo(message, LogLevel.Info));
            });
            Logger.Info(message);
        }
    }
}
