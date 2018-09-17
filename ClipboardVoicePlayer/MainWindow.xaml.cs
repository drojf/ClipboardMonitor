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

        readonly string SCAN_PATH = @"C:\games\Steam\steamapps\common\Umineko Chiru 2018-05-19\voice";

        public MainWindow()
        {
            InitializeComponent();

            player = new EasyCSCorePlayer();

            clipboardMonitor = new ClipboardMonitor();

            folderScanner = new FolderScanner();
            folderScanner.ScanFolder(SCAN_PATH);

            //setup 1s callback
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick1Second);
            dispatcherTimer.Interval = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500);
            dispatcherTimer.Start();
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

            bool fileFound = folderScanner.TryGetValue(clipboard, out string filePath);
            if (fileFound)
            {
                player.PlayAudio(filePath);
            }
            else
            {
                Console.WriteLine($"Couldn't find : [{clipboard}]");
            }
        }
    }
}
