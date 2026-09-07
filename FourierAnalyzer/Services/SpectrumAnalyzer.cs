using System;
using System.Diagnostics;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace FourierAnalyzer.Services
{
    /// <summary>
    /// Результат спектрального анализа: массив частот и соответствующий
    /// им амплитудный спектр, а также сведения о главной гармонике.
    /// </summary>
    public sealed class SpectrumResult
    {
        public double[] Frequencies { get; set; }

        public double[] Amplitudes { get; set; }

        public double PeakFrequency { get; set; }

        public double PeakAmplitude { get; set; }

        public int BlockLength { get; set; }

        public double FrequencyResolution { get; set; }
    }

    /// <summary>
    /// Результат сравнения библиотечного быстрого преобразования Фурье
    /// с собственной реализацией дискретного преобразования.
    /// </summary>
    public sealed class DftComparisonResult
    {
        public int Length { get; set; }

        public double MaxAbsoluteDifference { get; set; }

        public double MaxRelativeDifference { get; set; }

        public long LibraryMilliseconds { get; set; }

        public long NaiveMilliseconds { get; set; }
    }

    /// <summary>
    /// Класс, выполняющий преобразование Фурье и построение амплитудного спектра.
    /// Реализует все функции, требуемые заданием:
    /// 1) вычисление преобразования для массива double[] и возврат массива
    ///    комплексных чисел, представляющих преобразованный сигнал;
    /// 2) вычисление и возврат амплитудного спектра;
    /// 3) построение соответствия частот и амплитуд для графика;
    /// 4) подготовка данных для визуализации;
    /// дополнительно — собственный алгоритм ДПФ и сравнение его результатов
    /// с результатами библиотеки MathNet.Numerics.
    /// </summary>
    public sealed class SpectrumAnalyzer
    {
        /// <summary>Максимальная длина блока для быстрого преобразования (2^16 отсчётов).</summary>
        public const int MaxFftLength = 65536;

        /// <summary>Максимальная длина блока для собственного ДПФ: алгоритм имеет сложность O(N^2).</summary>
        public const int MaxNaiveLength = 2048;

        /// <summary>
        /// Функция 1. Вычисляет преобразование Фурье для массива вещественных отсчётов
        /// и возвращает массив комплексных чисел, представляющих преобразованный сигнал.
        /// Используется быстрое преобразование из MathNet.Numerics.
        /// Вариант FourierOptions.Matlab соответствует классическому определению:
        /// прямое преобразование выполняется без масштабирования, показатель степени
        /// экспоненты отрицательный.
        /// </summary>
        public Complex[] Transform(double[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }

            if (samples.Length == 0)
            {
                throw new ArgumentException("Массив отсчётов пуст", "samples");
            }

            Complex[] spectrum = new Complex[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                spectrum[i] = new Complex(samples[i], 0.0);
            }

            Fourier.Forward(spectrum, FourierOptions.Matlab);

            return spectrum;
        }

        /// <summary>
        /// Функция 2. Вычисляет и возвращает амплитудный спектр сигнала.
        /// Возвращается только первая половина отсчётов: спектр вещественного сигнала
        /// симметричен относительно частоты Найквиста, поэтому вторая половина
        /// не несёт новой информации. Амплитуды приведены к масштабу исходного
        /// сигнала множителем 2/N (для нулевой гармоники — 1/N).
        /// </summary>
        public double[] AmplitudeSpectrum(Complex[] spectrum)
        {
            if (spectrum == null)
            {
                throw new ArgumentNullException("spectrum");
            }

            int length = spectrum.Length;
            int half = length / 2;
            double[] amplitudes = new double[half];

            for (int k = 0; k < half; k++)
            {
                double magnitude = spectrum[k].Magnitude;
                amplitudes[k] = k == 0 ? magnitude / length : 2.0 * magnitude / length;
            }

            return amplitudes;
        }

        /// <summary>
        /// Функция 3. Строит массив частот в герцах, соответствующий отсчётам
        /// амплитудного спектра: f[k] = k * Fs / N.
        /// </summary>
        public double[] FrequencyScale(int spectrumLength, int blockLength, int sampleRate)
        {
            double[] frequencies = new double[spectrumLength];
            double resolution = (double)sampleRate / blockLength;

            for (int k = 0; k < spectrumLength; k++)
            {
                frequencies[k] = k * resolution;
            }

            return frequencies;
        }

        /// <summary>
        /// Функция 3-4. Полный цикл анализа: выделение блока, оконное взвешивание,
        /// преобразование Фурье, амплитудный спектр, шкала частот и поиск
        /// главной гармоники. Результат готов для передачи в средство визуализации.
        /// </summary>
        public SpectrumResult BuildSpectrum(double[] samples, int sampleRate)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentException("Частота дискретизации должна быть положительной", "sampleRate");
            }

            double[] block = TakePowerOfTwoBlock(samples, MaxFftLength);
            double[] windowed = ApplyHannWindow(block);

            Complex[] spectrum = Transform(windowed);
            double[] amplitudes = AmplitudeSpectrum(spectrum);

            // Компенсация ослабления сигнала окном Ханна (когерентное усиление 0,5)
            for (int i = 0; i < amplitudes.Length; i++)
            {
                amplitudes[i] *= 2.0;
            }

            double[] frequencies = FrequencyScale(amplitudes.Length, block.Length, sampleRate);

            int peakIndex = 0;
            double peakValue = 0.0;

            // Нулевую гармонику (постоянную составляющую) при поиске пика пропускаем
            for (int i = 1; i < amplitudes.Length; i++)
            {
                if (amplitudes[i] > peakValue)
                {
                    peakValue = amplitudes[i];
                    peakIndex = i;
                }
            }

            return new SpectrumResult
            {
                Frequencies = frequencies,
                Amplitudes = amplitudes,
                PeakFrequency = frequencies[peakIndex],
                PeakAmplitude = peakValue,
                BlockLength = block.Length,
                FrequencyResolution = (double)sampleRate / block.Length
            };
        }

        /// <summary>
        /// Собственная реализация дискретного преобразования Фурье по формуле
        /// X[k] = SUM( x[n] * e^(-j * 2 * PI * k * n / N) ), n = 0, 1, ..., N-1.
        /// Алгоритм прямой, его вычислительная сложность составляет O(N^2),
        /// поэтому он применим только к коротким сигналам.
        /// </summary>
        public Complex[] TransformNaive(double[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }

            int n = samples.Length;
            Complex[] result = new Complex[n];

            for (int k = 0; k < n; k++)
            {
                double real = 0.0;
                double imaginary = 0.0;

                for (int i = 0; i < n; i++)
                {
                    double angle = -2.0 * Math.PI * k * i / n;
                    real += samples[i] * Math.Cos(angle);
                    imaginary += samples[i] * Math.Sin(angle);
                }

                result[k] = new Complex(real, imaginary);
            }

            return result;
        }

        /// <summary>
        /// Сравнение собственного алгоритма с библиотечным: вычисляются оба
        /// преобразования одного и того же сигнала, находится наибольшее расхождение
        /// и измеряется время работы каждого алгоритма.
        /// </summary>
        public DftComparisonResult Compare(double[] samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException("samples");
            }

            double[] block = TakePowerOfTwoBlock(samples, MaxNaiveLength);

            Stopwatch watch = Stopwatch.StartNew();
            Complex[] library = Transform(block);
            watch.Stop();
            long libraryTime = watch.ElapsedMilliseconds;

            watch.Restart();
            Complex[] naive = TransformNaive(block);
            watch.Stop();
            long naiveTime = watch.ElapsedMilliseconds;

            double maxAbsolute = 0.0;
            double maxRelative = 0.0;

            for (int k = 0; k < block.Length; k++)
            {
                double difference = (library[k] - naive[k]).Magnitude;

                if (difference > maxAbsolute)
                {
                    maxAbsolute = difference;
                }

                double magnitude = library[k].Magnitude;
                if (magnitude > 1e-9)
                {
                    double relative = difference / magnitude;
                    if (relative > maxRelative)
                    {
                        maxRelative = relative;
                    }
                }
            }

            return new DftComparisonResult
            {
                Length = block.Length,
                MaxAbsoluteDifference = maxAbsolute,
                MaxRelativeDifference = maxRelative,
                LibraryMilliseconds = libraryTime,
                NaiveMilliseconds = naiveTime
            };
        }

        /// <summary>
        /// Выделяет из сигнала блок, длина которого равна степени двойки.
        /// Алгоритм быстрого преобразования наиболее эффективен именно на таких длинах.
        /// </summary>
        public static double[] TakePowerOfTwoBlock(double[] samples, int maxLength)
        {
            int length = 1;

            while (length * 2 <= samples.Length && length * 2 <= maxLength)
            {
                length *= 2;
            }

            if (length > samples.Length)
            {
                length = samples.Length;
            }

            double[] block = new double[length];
            Array.Copy(samples, block, length);

            return block;
        }

        /// <summary>
        /// Оконная функция Ханна: w[n] = 0,5 * (1 - cos(2*PI*n / (N-1))).
        /// Плавно приводит к нулю края блока и тем самым устраняет растекание
        /// спектра, возникающее из-за разрыва сигнала на границах блока.
        /// </summary>
        public static double[] ApplyHannWindow(double[] samples)
        {
            int n = samples.Length;
            double[] result = new double[n];

            if (n < 2)
            {
                Array.Copy(samples, result, n);
                return result;
            }

            for (int i = 0; i < n; i++)
            {
                double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (n - 1)));
                result[i] = samples[i] * window;
            }

            return result;
        }
    }
}
