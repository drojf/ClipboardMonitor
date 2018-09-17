using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardVoicePlayer
{
    class ClipboardMonitor
    {
        UInt32 lastClipboardSequenceNumber;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern UInt32 GetClipboardSequenceNumber();

        public ClipboardMonitor()
        {
            lastClipboardSequenceNumber = GetClipboardSequenceNumber();
        }

        //if the clipboard sequence number has changed (eg ctrl-c has been pressed), return true
        public bool ClipboardChanged()
        {
            //these variables are const
            UInt32 clipboardSequenceNumber = GetClipboardSequenceNumber();
            bool clipboardChanged = clipboardSequenceNumber == lastClipboardSequenceNumber;

            //remember the last clipboard sequence number to detect changes
            lastClipboardSequenceNumber = clipboardSequenceNumber;

            return clipboardChanged;
        }

    }
}
