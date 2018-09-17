using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardVoicePlayer
{
    class EasyCSCorePlayer
    {
        IWaveSource source;
        WasapiOut soundOut;

        public EasyCSCorePlayer()
        {
            //Register the new codec so can play back ogg vorbis files
            CodecFactory.Instance.Register("ogg-vorbis", new CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));
        }

        public void PlayAudio(string audioPath)
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

            Console.WriteLine($"Playing: [{audioPath}]");

            source = CodecFactory.Instance.GetCodec(audioPath);
            soundOut = new WasapiOut();

            soundOut.Initialize(source);
            soundOut.Play();
        }
    }
}
