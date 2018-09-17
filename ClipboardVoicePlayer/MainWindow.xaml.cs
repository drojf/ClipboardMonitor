using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore;
using CSCore.CoreAudioAPI;
using Microsoft.Win32;

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

        IWaveSource source;
        WasapiOut soundOut;

        readonly string SCAN_PATH = @"C:\games\Steam\steamapps\common\Umineko Chiru 2018-05-19\voice";

        public MainWindow()
        {
            InitializeComponent();

            clipboardMonitor = new ClipboardMonitor();

            folderScanner = new FolderScanner();
            folderScanner.ScanFolder(SCAN_PATH);

            //Register the new codec so can play back ogg vorbis files
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

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
                //clean up the old playback objects
                if (source != null)
                {
                    source.Dispose();
                }

                if (soundOut != null)
                {
                    soundOut.Dispose();
                }

                Console.WriteLine($"Playing: [{filePath}]");

                source = CodecFactory.Instance.GetCodec(filePath);
                soundOut = new WasapiOut();

                soundOut.Initialize(source);
                soundOut.Play();
            }
            else
            {
                Console.WriteLine($"Couldn't find : [{clipboard}]");
            }
        }
    }
}
