using System.Windows;
using FourierAnalyzer.Services;
using FourierAnalyzer.ViewModels;

namespace FourierAnalyzer
{
    /// <summary>
    /// Точка входа приложения: создаются загрузчик звука, сервис выбора файла
    /// и модель представления, после чего запускается главное окно.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            IAudioLoader audioLoader = new NAudioLoader();
            IFileDialogService fileDialogService = new FileDialogService();

            MainViewModel viewModel = new MainViewModel(audioLoader, fileDialogService);

            Views.MainWindow window = new Views.MainWindow();
            window.DataContext = viewModel;

            MainWindow = window;
            window.Show();
        }
    }
}
