using System;
using System.Threading.Tasks;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using FourierAnalyzer.Services;

namespace FourierAnalyzer.ViewModels
{
    /// <summary>
    /// Модель представления главного окна.
    /// Управляет выбором файла, запускает спектральный анализ в фоновом потоке
    /// и готовит модели графиков для визуализации библиотекой OxyPlot.
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        /// <summary>Ограничение на длительность читаемого фрагмента, секунд.</summary>
        private const int MaxSeconds = 30;

        /// <summary>Предельное число точек на графике: больше экран всё равно не покажет.</summary>
        private const int MaxPlotPoints = 4000;

        private readonly IAudioLoader _audioLoader;
        private readonly IFileDialogService _fileDialogService;
        private readonly SpectrumAnalyzer _analyzer;

        private AudioData _audioData;
        private SpectrumResult _spectrum;

        private string _filePath;
        private string _fileInfo;
        private string _spectrumInfo;
        private string _comparisonInfo;
        private string _statusMessage;
        private bool _isBusy;

        private PlotModel _amplitudeModel;
        private PlotModel _frequencyModel;
        private PlotModel _spectrumModel;

        public MainViewModel(IAudioLoader audioLoader, IFileDialogService fileDialogService)
        {
            if (audioLoader == null)
            {
                throw new ArgumentNullException("audioLoader");
            }

            if (fileDialogService == null)
            {
                throw new ArgumentNullException("fileDialogService");
            }

            _audioLoader = audioLoader;
            _fileDialogService = fileDialogService;
            _analyzer = new SpectrumAnalyzer();

            SelectFileCommand = new RelayCommand(parameter => SelectFile(), parameter => !IsBusy);
            CompareCommand = new RelayCommand(parameter => CompareAlgorithms(), parameter => CanCompare());

            AmplitudeModel = CreateEmptyModel("Амплитуды преобразования Фурье", "Номер отсчёта", "Амплитуда");
            FrequencyModel = CreateEmptyModel("Частота преобразования Фурье", "Номер отсчёта", "Частота, Гц");
            SpectrumModel = CreateEmptyModel("Амплитудный спектр сигнала", "Частота, Гц", "Амплитуда");

            StatusMessage = "Выберите звуковой файл для анализа";
            FileInfo = "Файл не выбран";
            SpectrumInfo = string.Empty;
            ComparisonInfo = "Сравнение не выполнялось";
        }

        #region Свойства

        public string FilePath
        {
            get { return _filePath; }
            private set { SetProperty(ref _filePath, value); }
        }

        public string FileInfo
        {
            get { return _fileInfo; }
            private set { SetProperty(ref _fileInfo, value); }
        }

        public string SpectrumInfo
        {
            get { return _spectrumInfo; }
            private set { SetProperty(ref _spectrumInfo, value); }
        }

        public string ComparisonInfo
        {
            get { return _comparisonInfo; }
            private set { SetProperty(ref _comparisonInfo, value); }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty(ref _statusMessage, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetProperty(ref _isBusy, value); }
        }

        /// <summary>График амплитуд преобразования Фурье по номерам отсчётов.</summary>
        public PlotModel AmplitudeModel
        {
            get { return _amplitudeModel; }
            private set { SetProperty(ref _amplitudeModel, value); }
        }

        /// <summary>График шкалы частот, соответствующей отсчётам спектра.</summary>
        public PlotModel FrequencyModel
        {
            get { return _frequencyModel; }
            private set { SetProperty(ref _frequencyModel, value); }
        }

        /// <summary>Основной график: зависимость амплитуды от частоты в герцах.</summary>
        public PlotModel SpectrumModel
        {
            get { return _spectrumModel; }
            private set { SetProperty(ref _spectrumModel, value); }
        }

        public ICommand SelectFileCommand { get; private set; }

        public ICommand CompareCommand { get; private set; }

        #endregion

        #region Команды

        private bool CanCompare()
        {
            return !IsBusy && _audioData != null;
        }

        private async void SelectFile()
        {
            string path = _fileDialogService.OpenAudioFile();

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FilePath = path;
            IsBusy = true;
            StatusMessage = "Чтение файла и вычисление преобразования Фурье...";
            ComparisonInfo = "Сравнение не выполнялось";

            try
            {
                // Чтение файла и расчёт выполняются в фоновом потоке,
                // чтобы окно приложения не «замерзало» на время вычислений
                AudioData audio = await Task.Run(() => _audioLoader.Load(path, MaxSeconds));
                SpectrumResult spectrum = await Task.Run(() => _analyzer.BuildSpectrum(audio.Samples, audio.SampleRate));

                _audioData = audio;
                _spectrum = spectrum;

                UpdateFileInfo(audio);
                UpdateSpectrumInfo(audio, spectrum);
                BuildPlots(spectrum);

                StatusMessage = "Анализ выполнен";
            }
            catch (Exception exception)
            {
                _audioData = null;
                _spectrum = null;
                FileInfo = "Не удалось обработать файл";
                SpectrumInfo = string.Empty;
                StatusMessage = "Ошибка: " + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void CompareAlgorithms()
        {
            if (_audioData == null)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Сравнение библиотечного БПФ и собственного ДПФ...";

            try
            {
                double[] samples = _audioData.Samples;
                DftComparisonResult result = await Task.Run(() => _analyzer.Compare(samples));

                ComparisonInfo = string.Format(
                    "Длина блока N = {0}. Наибольшее абсолютное расхождение: {1:E3}. " +
                    "Наибольшее относительное расхождение: {2:E3}. " +
                    "Время: БПФ (MathNet) {3} мс, собственное ДПФ {4} мс.",
                    result.Length,
                    result.MaxAbsoluteDifference,
                    result.MaxRelativeDifference,
                    result.LibraryMilliseconds,
                    result.NaiveMilliseconds);

                StatusMessage = "Сравнение выполнено";
            }
            catch (Exception exception)
            {
                ComparisonInfo = "Ошибка сравнения: " + exception.Message;
                StatusMessage = "Ошибка сравнения";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Формирование текстовых сведений и графиков

        private void UpdateFileInfo(AudioData audio)
        {
            FileInfo = string.Format(
                "{0}   |   частота дискретизации {1} Гц   |   каналов: {2}   |   разрядность {3} бит   |   длительность {4:mm\\:ss\\.fff}",
                audio.FileName,
                audio.SampleRate,
                audio.Channels,
                audio.BitsPerSample,
                audio.Duration);
        }

        private void UpdateSpectrumInfo(AudioData audio, SpectrumResult spectrum)
        {
            SpectrumInfo = string.Format(
                "Длина блока N = {0} отсчётов   |   разрешение по частоте {1:F2} Гц   |   " +
                "верхняя граница спектра {2:F0} Гц   |   главная гармоника {3:F1} Гц (амплитуда {4:F4})",
                spectrum.BlockLength,
                spectrum.FrequencyResolution,
                audio.SampleRate / 2.0,
                spectrum.PeakFrequency,
                spectrum.PeakAmplitude);
        }

        private static PlotModel CreateEmptyModel(string title, string xTitle, string yTitle)
        {
            PlotModel model = new PlotModel
            {
                Title = title,
                TitleFontSize = 14,
                PlotAreaBorderColor = OxyColors.LightGray
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xTitle,
                MajorGridlineStyle = LineStyle.Dot,
                MinorTickSize = 0
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = yTitle,
                MajorGridlineStyle = LineStyle.Dot,
                MinorTickSize = 0
            });

            return model;
        }

        /// <summary>
        /// Функция 4 задания: визуализация результатов.
        /// Строятся три графика — амплитуды по номерам отсчётов, шкала частот
        /// и основной амплитудный спектр в координатах «частота — амплитуда».
        /// </summary>
        private void BuildPlots(SpectrumResult spectrum)
        {
            AmplitudeModel = CreateSeriesModel(
                "Амплитуды преобразования Фурье",
                "Номер отсчёта",
                "Амплитуда",
                null,
                spectrum.Amplitudes,
                OxyColors.ForestGreen);

            FrequencyModel = CreateSeriesModel(
                "Частота преобразования Фурье",
                "Номер отсчёта",
                "Частота, Гц",
                null,
                spectrum.Frequencies,
                OxyColors.ForestGreen);

            SpectrumModel = CreateSeriesModel(
                "Амплитудный спектр сигнала",
                "Частота, Гц",
                "Амплитуда",
                spectrum.Frequencies,
                spectrum.Amplitudes,
                OxyColors.SteelBlue);
        }

        /// <summary>
        /// Создаёт модель графика. Если точек больше, чем может отобразить экран,
        /// данные прореживаются: из каждой группы берётся максимальное значение,
        /// поэтому пики спектра не теряются.
        /// </summary>
        private static PlotModel CreateSeriesModel(string title, string xTitle, string yTitle,
                                                   double[] xValues, double[] yValues, OxyColor color)
        {
            PlotModel model = CreateEmptyModel(title, xTitle, yTitle);

            LineSeries series = new LineSeries
            {
                Color = color,
                StrokeThickness = 1.0
            };

            int count = yValues.Length;
            int step = count > MaxPlotPoints ? count / MaxPlotPoints : 1;

            for (int i = 0; i < count; i += step)
            {
                int end = Math.Min(i + step, count);

                int bestIndex = i;
                double best = yValues[i];

                for (int j = i + 1; j < end; j++)
                {
                    if (yValues[j] > best)
                    {
                        best = yValues[j];
                        bestIndex = j;
                    }
                }

                double x = xValues != null ? xValues[bestIndex] : bestIndex;
                series.Points.Add(new DataPoint(x, best));
            }

            model.Series.Add(series);

            return model;
        }

        #endregion
    }
}
