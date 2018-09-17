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
    ///NOTE: this program is based on this cscore example: https://github.com/filoe/cscore/blob/master/Samples/NVorbisIntegration/Program.cs
    ///NVorbis is required to play back .ogg files.
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        }

        private string GetUserSelectedPath()
        {
            return PathComboBox.Text;
        }

        private void DispatcherTimer_Tick1Second(object sender, EventArgs e)
        {
            //only execute once for each 'copy'/ctrl-c press
            if (clipboardMonitor.ClipboardChanged())
            {
                return;
            }

            string clipboard = Clipboard.GetText();
            Console.WriteLine($"Detected Clipboard Change: [{clipboard}]");

            bool pathExists = false;
            try
            {
                string path = Path.Combine(GetUserSelectedPath(), clipboard);
                if(File.Exists(path))
                {
                    pathExists = true;
                    Console.WriteLine($"Trying to play: [{path}]");
                    player.PlayAudio(path);
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            if (!pathExists)
            {
                Console.WriteLine($"Couldn't find : [{clipboard}] in folder [{GetUserSelectedPath()}]");
            }
        }
    }
}
