using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardVoicePlayer
{
    class FolderScanner
    {
        Dictionary<string, string> filenameToPathDict = new Dictionary<string, string>();

        public void ScanFolder(string scanPath)
        {
            //scan folder for voice files
            foreach (string fullPath in Directory.EnumerateFiles(scanPath, "*.ogg", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(fullPath);
                if (filenameToPathDict.ContainsKey(filename))
                {
                    Console.WriteLine($"WARNING: [{filename}] at [{fullPath}] already exists at [{filenameToPathDict[filename]}]");
                }
                else
                {
                    filenameToPathDict.Add(filename, fullPath);
                }
                //Console.WriteLine($"Scanned [{filename} : {fullPath}]");
            }
        }

        public bool TryGetValue(string value, out string filePath)
        {
            bool result = filenameToPathDict.TryGetValue(value, out string _filePath);
            filePath = _filePath;
            return result;
        }
    }
}
