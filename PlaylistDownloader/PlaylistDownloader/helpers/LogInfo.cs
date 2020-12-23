using NLog;

namespace PlaylistDownloader.helpers {
    public class LogInfo {
        public string Message {
            get;
            set;
        }
        public LogLevel Level {
            get;
            set;
        }

        public LogInfo(string message, LogLevel level) {
            this.Message = message;
            this.Level = level;
        }
    }
}
