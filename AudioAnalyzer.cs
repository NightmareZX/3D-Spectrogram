using NAudio.Dsp;
using NAudio.Wave;
using Spectrogram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinForms_TestApp
{
    public class AudioAnalyzer : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly BufferedWaveProvider buffer;
        public readonly SpectrogramGenerator generator;
        private readonly int fftSize = 2048;  // Size for FFT
        private float[] previousByteData; // Stores previous data for smoothing
        private readonly Complex[] fftBuffer;
        private readonly float[] audioBuffer;
        private int bufferIndex = 0;
        private float smoothingTimeConstant;

        public AudioAnalyzer(ISampleProvider source, BufferedWaveProvider buffer)
        {
            this.source = source;
            this.buffer = buffer;
            fftBuffer = new Complex[fftSize];
            audioBuffer = new float[fftSize];
            previousByteData = new float[fftSize / 2];
            generator = new(source.WaveFormat.SampleRate, fftSize, 500);
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // Read audio data from the source
            int samplesRead = source.Read(buffer, offset, count);
            generator.Add(Array.ConvertAll(buffer, x => (double)x), false);
            // Write the audio data to the buffer (for playback)
            for (int i = 0; i < samplesRead; i++)
            {
                if (bufferIndex < fftSize)
                {
                    audioBuffer[bufferIndex] = buffer[i + offset];
                    bufferIndex++;
                }
                if (bufferIndex >= fftSize)
                {
                    bufferIndex = 0;  // Reset buffer index
                }
            }
            return samplesRead;
        }
        private void ApplySmoothing(float[] currentData)
        {
            for (int i = 0; i < currentData.Length; i++)
            {
                //previousByteData[i] = previousByteData[i] * smoothingTimeConstant + currentData[i] * (1 - smoothingTimeConstant);
            }
        }
        public byte[] GetByteFrequencyData()
        {
            var spec = generator.Process();
            float[] magnitudes = new float[fftSize / 2];
            // Copy audio data into the FFT buffer
            for (int i = 0; i < fftSize; i++)
            {
                fftBuffer[i].X = (float)(audioBuffer[i] * FastFourierTransform.HannWindow(i, fftSize)); // Apply window function
                fftBuffer[i].Y = 0;
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(fftSize, 2), fftBuffer);

            // Output frequency magnitudes (or use for visualization)
            for (int i = 0; i < fftBuffer.Length / 2; i++)
            {
                float magnitude = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                magnitudes[i] = magnitude;
            }
            float maxMagnitude = magnitudes.Max();

            byte[] byteFrequencyData = new byte[magnitudes.Length];


            for (int i = 0; i < magnitudes.Length; i++)
            {
                byteFrequencyData[i] = (byte)(magnitudes[i] / maxMagnitude * 255);
            }

            //ApplySmoothing(byteFrequencyData.Select(b => (float)b).ToArray());

            //for (int i = 0; i < previousByteData.Length; i++)
            //{
            //    byteFrequencyData[i] = (byte)previousByteData[i];
            //}

            return byteFrequencyData;
        }
    }
}
