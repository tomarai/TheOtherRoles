using System.IO;
using UnityEngine;

namespace TheOtherRoles
{
    public class FileImporter
    {
        public static AudioClip ImportWAVAudio(string filePath, bool _3d)
        {
            WAVImporter wav = new WAVImporter(filePath);
            AudioClip audioClip = AudioClip.Create(Path.GetFileName(filePath), wav.SampleCount, 1, wav.Frequency, _3d, false);
            audioClip.SetData(wav.LeftChannel, 0);
            return audioClip;
        }
    }
}