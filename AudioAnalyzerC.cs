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
    public class AudioAnalyzerC : ISampleProvider
    {
        private readonly ISampleProvider provider;
        private readonly WaveStream stream;
        private readonly int fftSize = 2048;  // Size for FFT
        private readonly object bufferlock = new();
        private Complex[] audioData = [];
        private Complex[] previousData = [];

        private float[] magnitudeData;
        private long dataSize = 0;
        private int totalSamplesRead = 0;
        public float SmoothingTimeConstant { get; set; } = 0.0f;
        public float MinDecibels { get; set; } = -100f;
        public float MaxDecibels { get; set; } = -30f;
        public int FrequencyBinCount => fftSize / 2;
        public int RanOutCounter { get; private set; } = 0;
        public float Gain {  get; set; } = 1.0f;


        public AudioAnalyzerC(WaveStream source, int fft)
        {
            stream = source;
            provider = source.ToSampleProvider();
            fftSize = fft;
            magnitudeData = new float[fftSize];

            dataSize = source.Length / (source.WaveFormat.BitsPerSample / 8);
        }

        public WaveFormat WaveFormat => provider.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // Read audio data from the source
            lock (bufferlock)
            {
                int samplesRead = provider.Read(buffer, offset, count);
                previousData = (audioData.Length == 0) ? new Complex[samplesRead] : audioData;
                if (audioData is null || audioData.Length != buffer.Length)
                {
                    audioData = new Complex[samplesRead];
                }

                for (int i = 0; i < samplesRead; i++)
                {
                    audioData[i].X = buffer[i] * Gain;
                }
                totalSamplesRead += samplesRead;

                return samplesRead;
            }
        }

        private void DoFFTAnalysis(Complex[] process)
        {
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

            FFT(true, (int)Math.Log(fftSize, 2), process);

            //average the current fft magnitude results with the previous results
            for (int i = 0; i < process.Length; i++)
            {
                float scalarMagnitude = process[i].Abs() * magnitudeScale;
                magnitudeData[i] = smoothing * magnitudeData[i] + (1 - smoothing) * scalarMagnitude;
            }
        }
        public byte[] GetByteFrequencyData(long currentPosition)
        {
            byte[] byteFrequencyData = new byte[FrequencyBinCount];
            // Copy audio data into the FFT buffer
            Complex[] fftBuffer;
            lock (bufferlock)
            {
                if (audioData.Length == 0) return byteFrequencyData;
                fftBuffer = new Complex[fftSize];
                Complex[] currentBuffer = audioData;
                long samplePos = currentPosition / (stream.WaveFormat.BitsPerSample / 8);
                long bufferPos = samplePos - (totalSamplesRead - currentBuffer.Length);
                for (int i = 0; i < fftSize; i++)
                {
                    if(bufferPos < 0)
                    {
                        currentBuffer = previousData;
                        bufferPos = previousData.Length - 1;
                    }
                    fftBuffer[fftSize - 1 - i] = currentBuffer[bufferPos];
                    bufferPos--;
                }
            }

            DoFFTAnalysis(fftBuffer);

            float rangeScaleFactor = MaxDecibels == MinDecibels ? 1 : 1 / (MaxDecibels - MinDecibels);
            for (int i = 0; i < FrequencyBinCount; ++i)
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
        public static void FFT(bool forward, int m, Complex[] data)
        {
            int num = 1;
            for (int i = 0; i < m; i++)
            {
                num *= 2;
            }

            int num2 = num >> 1;
            int num3 = 0;
            for (int i = 0; i < num - 1; i++)
            {
                if (i < num3)
                {
                    float x = data[i].X;
                    float y = data[i].Y;
                    data[i].X = data[num3].X;
                    data[i].Y = data[num3].Y;
                    data[num3].X = x;
                    data[num3].Y = y;
                }

                int num4;
                for (num4 = num2; num4 <= num3; num4 >>= 1)
                {
                    num3 -= num4;
                }

                num3 += num4;
            }

            float num5 = -1f;
            float num6 = 0f;
            int num7 = 1;
            for (int j = 0; j < m; j++)
            {
                int num8 = num7;
                num7 <<= 1;
                float num9 = 1f;
                float num10 = 0f;
                for (num3 = 0; num3 < num8; num3++)
                {
                    for (int i = num3; i < num; i += num7)
                    {
                        int num11 = i + num8;
                        float num12 = num9 * data[num11].X - num10 * data[num11].Y;
                        float num13 = num9 * data[num11].Y + num10 * data[num11].X;
                        data[num11].X = data[i].X - num12;
                        data[num11].Y = data[i].Y - num13;
                        //if (float.IsNaN(data[num11].X)) throw new Exception("NAN");
                        data[i].X += num12;
                        data[i].Y += num13;
                        //if (float.IsNaN(data[i].X)) throw new Exception("NAN");
                    }

                    float num14 = num9 * num5 - num10 * num6;
                    num10 = num9 * num6 + num10 * num5;
                    num9 = num14;
                }

                num6 = (float)Math.Sqrt((1f - num5) / 2f);
                if (forward)
                {
                    num6 = 0f - num6;
                }

                num5 = (float)Math.Sqrt((1f + num5) / 2f);
            }

            if (forward)
            {
                for (int i = 0; i < num; i++)
                {
                    data[i].X /= num;
                    data[i].Y /= num;
                }
            }
        }
    }
}
