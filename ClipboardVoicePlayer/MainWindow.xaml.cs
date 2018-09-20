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
        public string AddExtension { get; set; }
    }

    ///NOTE: this program is based on this cscore example: https://github.com/filoe/cscore/blob/master/Samples/NVorbisIntegration/Program.cs
    ///NVorbis is required to play back .ogg files.
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //NOTE: file path regex doesn't support spaces, and requires a file extension to be present
        readonly static Regex FUZZY_FILE_PATH_REGEX = new Regex(@"(\w+[\\\/])+\w+(\.\w+)?");
        //Alternate regex which matches just a file without any slashes
        readonly static Regex FUZZY_FILE_REGEX = new Regex(@"\w+\.\w+");

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
            SetUserExtension(config.AddExtension);
        }

        private void SaveConfig()
        {
            Config config = new Config();
            config.ScanFolder = GetUserSelectedPath();
            config.AddExtension = GetUserExtension();
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

        private string GetUserExtension()
        {
            return AddExtensionComboBox.Text;
        }

        private void SetUserExtension(string ext)
        {
            AddExtensionComboBox.Text = ext;
        }

        private void ClearOutputTextBoxes()
        {
            ClipboardTextbox.Text = "N/A";
            GuessTextbox.Text = "N/A";
            FilePathTextBox.Text = "N/A";
        }

        //note: returns null if path could not be determined. Reason for exception is provided in the 'out' argument.
        //TODO: handle if multiple paths in clipboard?
        private string GetPathFromClipboardAndScanFolder(string scanFolder, string clipboard, out string errorReason, out string guess, string addExtension="")
        {
            //try both regexes
            Match match = FUZZY_FILE_PATH_REGEX.Match(clipboard);
            if(!match.Success)
            {
                match = FUZZY_FILE_REGEX.Match(clipboard);
            }

            guess = match.Value + addExtension;

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

        private string ModifyDigitsOfPath(string path, int difference)
        {
            //remove the file extension
            string[] splitPath = path.Split(new char[] { '.' }, count: 2);

            string extensionWithoutDot;
            string pathWithoutExtension;
            if(splitPath.Length == 1)
            {
                //no file extension
                extensionWithoutDot = "";
                pathWithoutExtension = path;
            }
            else
            {
                extensionWithoutDot = splitPath[1];
                pathWithoutExtension = splitPath[0];
            }

            //get the numbers at the end of the string
            SplitStringOnTrailingDigits(pathWithoutExtension, out string pathWithoutExtensionHead, out string trailingDigitsAsString);

            int trailingDigits = Int32.Parse(trailingDigitsAsString);

            return $"{pathWithoutExtensionHead}{trailingDigits + difference}.{extensionWithoutDot}";
        }

        //splits a string into the section before any trailing digits, and the trailing digits
        //TODO: need to handle the cases like: kum_1e65_i? just return false or with an error.
        public void SplitStringOnTrailingDigits(string s, out string head, out string trailingDigits)
        {
            //count from end of string to start of string, stopping at the first non-digit
            int firstNonDigitIndex = s.Length - 1;
            for (; firstNonDigitIndex >= 0; firstNonDigitIndex--)
            {
                if (!Char.IsDigit(s[firstNonDigitIndex]))
                {
                    break;
                }
            }

            head = s.Substring(0, firstNonDigitIndex + 1); //length of the non-digit section is lastNonDigitIndex + 1
            trailingDigits = s.Substring(firstNonDigitIndex + 1);            //the last non-digit is at index 'lastNonDigitIndex', therefore the first digit is at 'lastNonDigitIndex + 1'
        }

        private void ManualPlay_Click(object sender, RoutedEventArgs e)
        {
            TryPlayNextAudio();
        }

        private void TryPlayNextAudio()
        {
            try
            {
                player.PlayAudio(Path.Combine(GetUserSelectedPath(), PlayNextTextBox.Text));
            }
            catch
            {
                //do nothing for now if this fails
            }
        }

        private void IncrementManualPlayBox(string original)
        {
            string newPathToPlay = ModifyDigitsOfPath(original, 1);
            PlayNextTextBox.Text = newPathToPlay;
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

                ClearOutputTextBoxes();

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
                string pathToPlay = GetPathFromClipboardAndScanFolder(GetUserSelectedPath(), clipboard, out string errorReason, out string guess, addExtension:GetUserExtension());
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

                IncrementManualPlayBox(GuessTextbox.Text);            //update the 'next to play' box
            }
            catch(Exception unknownException)
            {
                FilePathTextBox.Text = $"Unknown Exception occured: {unknownException.ToString()}";
            }
        }

        private void NextAudio_Click(object sender, RoutedEventArgs e)
        {
            IncrementManualPlayBox(PlayNextTextBox.Text);
            TryPlayNextAudio();
        }
    }
}
