using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardVoicePlayer
{
    public class Config
    {
        public string ScanFolder { get; set; }
    }

    ///NOTE: this program is based on this cscore example: https://github.com/filoe/cscore/blob/master/Samples/NVorbisIntegration/Program.cs
    ///NVorbis is required to play back .ogg files.
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly static string TOML_PATH = "conf.toml";

        FolderScanner folderScanner;
        ClipboardMonitor clipboardMonitor;
        EasyCSCorePlayer player;

        public MainWindow()
        {
            InitializeComponent();

            player = new EasyCSCorePlayer();

            clipboardMonitor = new ClipboardMonitor();

            //setup 1s callback
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick1Second);
            dispatcherTimer.Interval = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500);
            dispatcherTimer.Start();

            LoadConfigFromFileOrDefault(TOML_PATH);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Console.WriteLine("Window Closing called");
            this.SaveConfig();
        }

        void OnProcessExit(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private void LoadConfigFromFileOrDefault(string path)
        {
            Config config;
            if (File.Exists(path))
            {
                config = Nett.Toml.ReadFile<Config>(path);
            }
            else
            {
                config = new Config();
            }

            //restore GUI with config
            SetUserSelectedPath(config.ScanFolder);

        }

        private void SaveConfig()
        {
            Config config = new Config();
            config.ScanFolder = GetUserSelectedPath();
            Nett.Toml.WriteFile(config, TOML_PATH);
        }

        private string GetUserSelectedPath()
        {
            return PathComboBox.Text;
        }

        private void SetUserSelectedPath(string path)
        {
            PathComboBox.Text = path;
        }

        private void DispatcherTimer_Tick1Second(object sender, EventArgs e)
        {
            //only execute once for each 'copy'/ctrl-c press
            if (clipboardMonitor.ClipboardChanged())
            {
                return;
            }

            string clipboard = Clipboard.GetText();
            ClipboardTextbox.Text = clipboard;
            //Console.WriteLine($"Detected Clipboard Change: [{clipboard}]");
            string pathMessage = "Error";
            bool pathExists = false;
            try
            {
                string path = Path.Combine(GetUserSelectedPath(), clipboard);
                if(File.Exists(path))
                {
                    pathExists = true;
                    pathMessage = $"{path}";
                    player.PlayAudio(path);
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            if (!pathExists)
            {
                pathMessage = $"Couldn't find: [{GetUserSelectedPath()}]/[{clipboard}]";
            }

            FilePathTextBox.Text = pathMessage;
        }

    }
}
