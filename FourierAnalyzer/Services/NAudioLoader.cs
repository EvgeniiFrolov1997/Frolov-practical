using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace FourierAnalyzer.Services
{
    /// <summary>
    /// Результат чтения звукового файла: отсчёты, приведённые к моно,
    /// и параметры исходной записи.
    /// </summary>
    public sealed class AudioData
    {
        public double[] Samples { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public TimeSpan Duration { get; set; }

        public string FileName { get; set; }

        public int BitsPerSample { get; set; }
    }

    /// <summary>Контракт загрузчика звуковых файлов.</summary>
    public interface IAudioLoader
    {
        AudioData Load(string path, int maxSeconds);
    }

    /// <summary>
    /// Загрузчик звуковых файлов на основе библиотеки NAudio.
    /// Класс AudioFileReader самостоятельно определяет формат файла (wav, mp3, aiff)
    /// и выдаёт отсчёты в виде вещественных чисел в диапазоне от -1 до 1,
    /// что удобно для дальнейшего спектрального анализа.
    /// </summary>
    public sealed class NAudioLoader : IAudioLoader
    {
        /// <summary>
        /// Читает файл и приводит сигнал к одному каналу усреднением по каналам.
        /// Параметр maxSeconds ограничивает объём читаемых данных.
        /// </summary>
        public AudioData Load(string path, int maxSeconds)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Не задан путь к файлу", "path");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Звуковой файл не найден", path);
            }

            using (AudioFileReader reader = new AudioFileReader(path))
            {
                WaveFormat format = reader.WaveFormat;
                int channels = format.Channels;
                int sampleRate = format.SampleRate;

                if (channels <= 0 || sampleRate <= 0)
                {
                    throw new InvalidOperationException("Не удалось определить формат звукового файла");
                }

                int maxFrames = maxSeconds > 0 ? maxSeconds * sampleRate : int.MaxValue;

                List<double> mono = new List<double>();
                float[] buffer = new float[sampleRate * channels];
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i + channels <= read; i += channels)
                    {
                        double sum = 0.0;

                        for (int c = 0; c < channels; c++)
                        {
                            sum += buffer[i + c];
                        }

                        mono.Add(sum / channels);
                    }

                    if (mono.Count >= maxFrames)
                    {
                        break;
                    }
                }

                if (mono.Count == 0)
                {
                    throw new InvalidOperationException("Файл не содержит звуковых данных");
                }

                return new AudioData
                {
                    Samples = mono.ToArray(),
                    SampleRate = sampleRate,
                    Channels = channels,
                    Duration = reader.TotalTime,
                    BitsPerSample = format.BitsPerSample,
                    FileName = Path.GetFileName(path)
                };
            }
        }
    }
}
