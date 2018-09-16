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

namespace ClipboardVoicePlayer
{
    ///NOTE: this program is based on this cscore example: https://github.com/filoe/cscore/blob/master/Samples/NVorbisIntegration/Program.cs
    ///NVorbis is required to play back .ogg files.
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string SCAN_PATH = @"C:\temp\testfolder";

        string lastClipboardValue = String.Empty;
        Dictionary<string, string> filenameToPathDict = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            //Register the new codec so can play back ogg vorbis files
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            //scan folder for voice files
            foreach (string fullPath in Directory.EnumerateFiles(SCAN_PATH, "*.ogg", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(fullPath);
                filenameToPathDict.Add(filename, fullPath);
                Console.WriteLine($"Scanned [{filename} : {fullPath}]");
            }
            
            //setup 1s callback
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick1Second);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick1Second(object sender, EventArgs e)
        { 
            string clipboard = Clipboard.GetText();
            
            //only execute on transitions of clipboard
            if(clipboard == lastClipboardValue)
            {
                return;
            }
            else
            {
                lastClipboardValue = clipboard;
            }

            Console.WriteLine($"Clipboard: [{clipboard}]");

            bool fileFound = filenameToPathDict.TryGetValue(clipboard, out string filePath);
            if (fileFound)
            {
                Console.WriteLine($"Trying to play: [{filePath}]");

                using (var source = CodecFactory.Instance.GetCodec(filePath))
                {
                    using (WasapiOut soundOut = new WasapiOut())
                    {
                        soundOut.Initialize(source);
                        soundOut.Play();
                    }
                }
            }
            else
            {
                Console.WriteLine($"Couldn't find : [{clipboard}]");
            }
        }
    }
}
