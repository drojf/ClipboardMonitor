using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        //NOTE: file path regex doesn't support spaces, and requires a file extension to be present
        readonly static Regex FUZZY_FILE_PATH_REGEX = new Regex(@"(\w+\\)*\w+\.\w+");
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

        //note: returns null if path could not be determined. Reason for exception is provided in the 'out' argument.
        //TODO: handle if multiple paths in clipboard?
        private string GetPathFromClipboardAndScanFolder(string scanFolder, string clipboard, out string errorReason, out string guess)
        {
            Match match = FUZZY_FILE_PATH_REGEX.Match(clipboard);
            guess = match.Value;

            //check a path is found in the clipboard
            if(!match.Success || guess.Trim() == string.Empty)
            {
                errorReason = "Couldn't find a path in clipboard text";
                return null;
            }

            //check strings combine to a valid path
            string fullPath = null;
            try
            {
                fullPath = Path.Combine(GetUserSelectedPath(), guess);
            }
            catch(Exception e)
            {
                errorReason = $"Invalid combined path: [{GetUserSelectedPath()}]/[{guess}]";
                return null;
            }

            //check file exists on disk
            if (!File.Exists(fullPath))
            {
                errorReason = $"Path [{fullPath}] valid but couldn't be found on disk!";
                return null;
            }

            errorReason = $"Path valid and exists";
            return fullPath;
        }

        private void DispatcherTimer_Tick1Second(object sender, EventArgs e)
        {
            try
            {
                //only execute once for each 'copy'/ctrl-c press
                if (clipboardMonitor.ClipboardChanged())
                {
                    return;
                }

                //See https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
                string clipboard = null;
                try
                {
                    clipboard = Clipboard.GetText();
                    ClipboardTextbox.Text = clipboard;
                }
                catch(Exception clipboardException)
                {
                    FilePathTextBox.Text = $"Clipboard Exception occured: {clipboardException.ToString()}";
                    return;
                }

                //get path and check valid
                string pathToPlay = GetPathFromClipboardAndScanFolder(GetUserSelectedPath(), clipboard, out string errorReason, out string guess);
                if (pathToPlay == null)
                {
                    FilePathTextBox.Text = errorReason;
                    return;
                }
                GuessTextbox.Text = guess;

                //try to play the audio
                try
                {
                    player.PlayAudio(pathToPlay);
                }
                catch (Exception exception)
                {
                    FilePathTextBox.Text = $"Couldn't play audio {pathToPlay}: {exception.ToString()}";
                    return;
                }

                FilePathTextBox.Text = pathToPlay;
            }
            catch(Exception unknownException)
            {
                FilePathTextBox.Text = $"Unknown Exception occured: {unknownException.ToString()}";
            }
        }

    }
}
