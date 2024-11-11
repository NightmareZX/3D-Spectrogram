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
    public class AudioAnalyzerB : ISampleProvider
    {
        private readonly ISampleProvider source;
        BiQuadFilter bandpass;
        private readonly int fftSize = 2048;  // Size for FFT
        private float[] previousByteData; // Stores previous data for smoothing
        private int bufferIndex = 0;
        private object bufferlock = new();
        private readonly Queue<Complex[]> dataQueue = new();
        private Complex[] incompleteData;
        private int incompleteDataIndex;
        private float[] magnitudeData;
        public float SmoothingTimeConstant { get; set; } = 0.0f;
        public float MinDecibels { get; set; } = -100f;
        public float MaxDecibels { get; set; } = -30f;
        public int FrequencyBinCount => fftSize / 2;
        public int RanOutCounter { get; private set; } = 0;
        public float Gain {  get; set; } = 1.0f;


        public AudioAnalyzerB(ISampleProvider source, int fft, BiQuadFilter filter = null)
        {
            this.source = source;
            fftSize = fft;
            previousByteData = new float[fftSize / 2];
            magnitudeData = new float[fftSize];
            incompleteDataIndex = -1;
            bandpass = filter;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // Read audio data from the source
            lock (bufferlock)
            {
                int samplesRead = source.Read(buffer, offset, count);
                // Write the audio data to the buffer (for playback)
                int dataIndex = 0;
                Complex[] fftDataToAdd;

                //if there is incomplete data
                if (incompleteDataIndex != -1)
                {
                    fftDataToAdd = incompleteData;
                    dataIndex = incompleteDataIndex;
                }
                //if there is no incomplete data
                else
                {
                    fftDataToAdd = new Complex[fftSize];
                }
                incompleteDataIndex = -1;//reset incomplete index
                for (int i = 0; i < samplesRead; i++)
                {
                    //float sample = bandpass.Transform(buffer[i] * Gain);
                    fftDataToAdd[dataIndex].X = buffer[i] * Gain;

                    dataIndex++;
                    //if we are on the last sample but didnt fill the current buffer completely
                    if (i == samplesRead - 1 && dataIndex != fftSize)
                    {
                        incompleteDataIndex = dataIndex;
                        incompleteData = fftDataToAdd;
                    }
                    //if we are not on the last sample and we have filled
                    else if (i != samplesRead - 1 && dataIndex >= fftSize)
                    {
                        dataQueue.Enqueue(fftDataToAdd);
                        fftDataToAdd = new Complex[fftSize];
                        dataIndex = 0;
                    }
                }
                return samplesRead;
            }
        }
        private void DoFFTAnalysis(Complex[] process)
        {
            int fftSize = process.Length;
            if (process.Length <= 0) return;
            float smoothing = SmoothingTimeConstant;
            float magnitudeScale = 1.0f / fftSize;
            smoothing = MathF.Max(0.0f, smoothing);
            smoothing = MathF.Min(1.0f, smoothing);

            for (int i = 0; i < fftSize; i++)
            {
                //process[i].X *= (float)FastFourierTransform.HannWindow(i, fftSize);
                process[i].X *= (float)BlackmanWindow(i, fftSize);
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftSize, 2), process);

            //average the current fft magnitude results with the previous results
            for (int i = 0; i < process.Length; i++)
            {
                float scalarMagnitude = process[i].Abs() * magnitudeScale;
                magnitudeData[i] = smoothing * magnitudeData[i] + (1 - smoothing) * scalarMagnitude;
            }
        }
        public byte[] GetByteFrequencyData()
        {
            // Copy audio data into the FFT buffer
            Complex[]? fftBuffer;
            lock (bufferlock)
            {
                if (dataQueue.Count > 0)
                {
                    fftBuffer = dataQueue.Dequeue();
                }
                else
                {
                    fftBuffer = null;
                }
            }
            if(fftBuffer is null) return [];

            DoFFTAnalysis(fftBuffer);

            byte[] byteFrequencyData = new byte[FrequencyBinCount];
            float rangeScaleFactor = MaxDecibels == MinDecibels ? 1 : 1 / (MaxDecibels - MinDecibels);

            int freqBins = FrequencyBinCount;
            for (int i = 0; i < freqBins; ++i)
            {
                float dbMag = (magnitudeData[i] == 0.0f) ? MinDecibels : ExtensionMethods.LinearToDecibels(magnitudeData[i]);
                float scaledValue = byte.MaxValue * (dbMag - MinDecibels) * rangeScaleFactor;

                // Clip to valid range.
                if (scaledValue < 0) scaledValue = 0;
                if (scaledValue > byte.MaxValue) scaledValue = byte.MaxValue;

                byteFrequencyData[i] = (byte)scaledValue;
            }

            return byteFrequencyData;
        }

        public static double BlackmanWindow(int n, int frameSize)
        {
            double alpha = 0.16;
            double a0 = (1 - alpha) / 2;
            double a1 = 0.5;
            double a2 = alpha / 2;

            return a0
                   - a1 * Math.Cos((2 * Math.PI * n) / (frameSize - 1))
                   + a2 * Math.Cos((4 * Math.PI * n) / (frameSize - 1));
        }
    }
}
