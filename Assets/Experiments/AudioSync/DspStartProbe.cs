using System;
using System.Threading;
using UnityEngine;

namespace Reverb.Experiments.AudioSync
{
    // TEST_ONLY: first non-zero sample in each source's Unity audio-thread filter.
    // Different musical onsets/silence make this unsuitable as a general music-sync meter.
    public sealed class DspStartProbe : MonoBehaviour
    {
        private volatile bool armed;
        private double firstSignalDsp = double.NaN;
        private double notBefore;
        private int frequency;
        public double FirstSignalDsp => Volatile.Read(ref firstSignalDsp);
        public bool Captured => !double.IsNaN(FirstSignalDsp);
        public void Arm(double earliestDsp)
        {
            armed = false;
            frequency = AudioSettings.outputSampleRate;
            notBefore = earliestDsp;
            Volatile.Write(ref firstSignalDsp, double.NaN);
            armed = true;
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!armed || channels <= 0) return;
            double blockDsp = AudioSettings.dspTime;
            int firstFrame = Math.Max(0, (int)Math.Ceiling((notBefore - blockDsp) * frequency));
            if (firstFrame >= data.Length / channels) return;
            for (int i = firstFrame * channels; i < data.Length; i++)
            {
                if (Math.Abs(data[i]) < 0.000001f) continue;
                Volatile.Write(ref firstSignalDsp, blockDsp + (i / channels) / (double)frequency);
                armed = false;
                break;
            }
        }
    }
}
