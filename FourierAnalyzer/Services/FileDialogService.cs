using Microsoft.Win32;

namespace FourierAnalyzer.Services
{
    /// <summary>
    /// Контракт сервиса выбора файла. Нужен, чтобы модель представления
    /// не открывала системные окна напрямую — требование шаблона MVVM.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>Возвращает путь к выбранному файлу или null, если выбор отменён.</summary>
        string OpenAudioFile();
    }

    /// <summary>Реализация выбора файла стандартным диалогом Windows.</summary>
    public sealed class FileDialogService : IFileDialogService
    {
        public string OpenAudioFile()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Выберите звуковой файл для анализа",
                Filter = "Звуковые файлы (*.wav;*.mp3;*.aiff)|*.wav;*.mp3;*.aiff|" +
                         "Файлы WAV (*.wav)|*.wav|" +
                         "Файлы MP3 (*.mp3)|*.mp3|" +
                         "Все файлы (*.*)|*.*",
                CheckFileExists = true
            };

            bool? result = dialog.ShowDialog();

            return result == true ? dialog.FileName : null;
        }
    }
}
