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
        IWaveSource source;
        WasapiOut soundOut;

        readonly string SCAN_PATH = @"C:\games\Steam\steamapps\common\Umineko Chiru 2018-05-19\voice";

        UInt32 LastClipboardSequenceNumber;
        Dictionary<string, string> filenameToPathDict = new Dictionary<string, string>();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern UInt32 GetClipboardSequenceNumber();

        public MainWindow()
        {
            InitializeComponent();

            LastClipboardSequenceNumber = GetClipboardSequenceNumber();

            //Register the new codec so can play back ogg vorbis files
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            //scan folder for voice files
            foreach (string fullPath in Directory.EnumerateFiles(SCAN_PATH, "*.ogg", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(fullPath);
                if(filenameToPathDict.ContainsKey(filename))
                {
                    Console.WriteLine($"WARNING: [{filename}] at [{fullPath}] already exists at [{filenameToPathDict[filename]}]");
                }
                else
                {
                    filenameToPathDict.Add(filename, fullPath);
                }
                //Console.WriteLine($"Scanned [{filename} : {fullPath}]");
            }

            //setup 1s callback
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick1Second);
            dispatcherTimer.Interval = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick1Second(object sender, EventArgs e)
        {
            //only execute once for each 'copy'/ctrl-c press
            UInt32 clipboardSequenceNumber = GetClipboardSequenceNumber();
            if (clipboardSequenceNumber == LastClipboardSequenceNumber)
            {
                return;
            }
            else
            {
                LastClipboardSequenceNumber = clipboardSequenceNumber;
            }

            string clipboard = Clipboard.GetText();
            Console.WriteLine($"Detected Clipboard Change: [{clipboard}]");

            bool fileFound = filenameToPathDict.TryGetValue(clipboard, out string filePath);
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
