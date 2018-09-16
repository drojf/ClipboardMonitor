using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore;
using NVorbis;

namespace ClipboardVoicePlayer
{
    public sealed class NVorbisSource : ISampleSource
    {
        private readonly Stream _stream;
        private readonly VorbisReader _vorbisReader;

        private readonly WaveFormat _waveFormat;
        private bool _disposed;

        public NVorbisSource(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("Stream is not readable.", "stream");
            _stream = stream;
            _vorbisReader = new VorbisReader(stream, false);
            _waveFormat = new WaveFormat(_vorbisReader.SampleRate, 32, _vorbisReader.Channels, AudioEncoding.IeeeFloat);
        }

        public bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        //got fixed through workitem #17, thanks for reporting @rgodart.
        public long Length
        {
            get { return CanSeek ? (long)(_vorbisReader.TotalTime.TotalSeconds * _waveFormat.SampleRate * _waveFormat.Channels) : 0; }
        }

        //got fixed through workitem #17, thanks for reporting @rgodart.
        public long Position
        {
            get
            {
                return CanSeek ? (long)(_vorbisReader.DecodedTime.TotalSeconds * _vorbisReader.SampleRate * _vorbisReader.Channels) : 0;
            }
            set
            {
                if (!CanSeek)
                    throw new InvalidOperationException("NVorbisSource is not seekable.");
                if (value < 0 || value > Length)
                    throw new ArgumentOutOfRangeException("value");

                _vorbisReader.DecodedTime = TimeSpan.FromSeconds((double)value / _vorbisReader.SampleRate / _vorbisReader.Channels);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _vorbisReader.ReadSamples(buffer, offset, count);
        }

        public void Dispose()
        {
            if (!_disposed)
                _vorbisReader.Dispose();
            else
                throw new ObjectDisposedException("NVorbisSource");
            _disposed = true;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string lastClipboardValue = String.Empty;

        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        private void CleanupPlayback()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }

            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        public void OpenAndPlay(string filename)
        {
            CleanupPlayback();
            var enumerator = new MMDeviceEnumerator();
            MMDevice defaultAudioDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _waveSource = CodecFactory.Instance.GetCodec(filename)
                    .ToSampleSource()
                    .ToMono()
                    .ToWaveSource();
            _soundOut = new WasapiOut() { Latency = 100, Device = defaultAudioDevice };
            _soundOut.Initialize(_waveSource);
            _soundOut.Play();
        }

        readonly string SCAN_PATH = @"C:\temp\testfolder";
        Dictionary<string, string> filenameToPathDict = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            //scan folder for voice files
            foreach (string fullPath in Directory.EnumerateFiles(SCAN_PATH, "*.*", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(fullPath);
                filenameToPathDict.Add(filename, fullPath);
                Console.WriteLine($"Scanned [{filename} : {fullPath}]");
            }

            //Register the new codec.
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));
            
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
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

                /*using (var vorbisStream = new NAudio.Vorbis.VorbisWaveReader(filePath))
                using (var waveOut = new NAudio.Wave.WaveOutEvent())
                {
                    waveOut.Init(vorbisStream);
                    waveOut.Play();

                    // wait here until playback stops or should stop
                }*/
                using (var source = CodecFactory.Instance.GetCodec(filePath))
                {
                    using (WasapiOut soundOut = new WasapiOut())
                    {
                        soundOut.Initialize(source);
                        soundOut.Play();
                    }
                }

                OpenAndPlay(filePath);
            }
            else
            {
                Console.WriteLine($"Couldn't find : [{clipboard}]");
            }
        }
    }
}
