using NAudio.Dsp;
using NAudio.Wave;
using Spectrogram;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Complex = NAudio.Dsp.Complex;

namespace WinForms_TestApp
{
    public class AudioAnalyzer : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly BufferedWaveProvider buffer;
        public readonly SpectrogramGenerator generator;
        private readonly int fftSize = 2048;  // Size for FFT
        private float[] previousByteData; // Stores previous data for smoothing
        private readonly float[] audioBuffer;
        private int bufferIndex = 0;
        private float smoothingTimeConstant;
        private object bufferlock = new();
        private float[] hannWindow;
        private Queue<Complex[]> dataQueue = new();
        private Complex[] incompleteData;
        private Complex[] lastData;
        private int incompleteDataIndex;

        public AudioAnalyzer(ISampleProvider source, BufferedWaveProvider buffer)
        {
            this.source = source;
            this.buffer = buffer;
            audioBuffer = new float[fftSize];
            previousByteData = new float[fftSize / 2];
            generator = new(source.WaveFormat.SampleRate, fftSize, 500);
            hannWindow = GenerateHannWindow();
            lastData = new Complex[fftSize];
            incompleteDataIndex = -1;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // Read audio data from the source
            lock (bufferlock)
            {
                int samplesRead = source.Read(buffer, offset, count);
                //generator.Add(Array.ConvertAll(buffer, x => (double)x), false);
                // Write the audio data to the buffer (for playback)
                int dataIndex = 0;
                Complex[] fftDataToAdd;
                if (incompleteDataIndex != -1)
                {
                    fftDataToAdd = incompleteData;
                    dataIndex = incompleteDataIndex;
                }
                else
                {
                    fftDataToAdd = new Complex[fftSize];
                }
                incompleteDataIndex = -1;
                for (int i = 0; i < samplesRead; i++)
                {
                    fftDataToAdd[dataIndex].X = buffer[i];

                    dataIndex++;
                    if (i == samplesRead - 1 && dataIndex != fftSize)
                    {
                        incompleteDataIndex = dataIndex;
                        incompleteData = fftDataToAdd;
                    }
                    else if (i != samplesRead - 1 && dataIndex >= fftSize)
                    {
                        dataQueue.Enqueue(fftDataToAdd);
                        fftDataToAdd = new Complex[fftSize];
                        dataIndex = 0;
                    }
                    //if (bufferIndex < fftSize)
                    //{
                    //    audioBuffer[bufferIndex] = buffer[i + offset];
                    //    bufferIndex++;
                    //}
                    //if (bufferIndex >= fftSize)
                    //{
                    //    bufferIndex = 0;  // Reset buffer index
                    //}
                }
                return samplesRead;
            }
        }
        private float[] GenerateHannWindow()
        {
            float[] window = new float[fftSize / 2];
            float angleUnit = 2 * MathF.PI / (window.Length - 1);
            for (int i = 0; i < window.Length; i++)
            {
                window[i] = 0.5f * (1 - MathF.Cos(i * angleUnit));
            }
            return window;
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
            //var spec = generator.Process();
            float[] magnitudes = new float[fftSize / 2];
            // Copy audio data into the FFT buffer
            Complex[] fftBuffer;
            int currentCount;
            lock (bufferlock)
            {
                if(dataQueue.Count > 0)
                {
                    fftBuffer = dataQueue.Dequeue();
                }
                else
                {
                    fftBuffer = lastData;
                }
                currentCount = dataQueue.Count;
            }
            if(currentCount == 0)
            {
                fftBuffer.CopyTo(lastData, 0);
            }

            for (int i = 0; i < fftSize; i++)
            {
                fftBuffer[i].X *= (float)FastFourierTransform.HannWindow(i, fftSize); // Apply window function
                //fftBuffer[i].X = (float)(audioBuffer[i] * FastFourierTransform.HannWindow(i, fftSize)); // Apply window function
                //fftBuffer[i].Y = 0;
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
