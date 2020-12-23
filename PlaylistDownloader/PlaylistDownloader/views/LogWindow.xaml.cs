using PlaylistDownloader.helpers;
using System.Collections.ObjectModel;
using System.Windows;

namespace PlaylistDownloader.views {
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window {
        public ObservableCollection<LogInfo> Logs {
            get {
                return PlaylistLogger.Logs;
            }
        }

        public LogWindow() {
            InitializeComponent();
            DataContext = this;
        }
    }
}
