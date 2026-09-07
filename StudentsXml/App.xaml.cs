using System;
using System.IO;
using System.Windows;
using StudentsXml.Services;
using StudentsXml.ViewModels;

namespace StudentsXml
{
    /// <summary>
    /// Точка входа приложения. Здесь выполняется «сборка» приложения:
    /// создаются хранилище, сервис диалогов и модель представления,
    /// после чего ViewModel назначается контекстом данных главного окна.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Имя файла данных, создаваемого программой.</summary>
        private const string DataFileName = "students.xml";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataFileName);

            IStudentRepository repository = new XmlStudentRepository(filePath);
            IDialogService dialogService = new DialogService();
            MainViewModel viewModel = new MainViewModel(repository, dialogService);

            Views.MainWindow window = new Views.MainWindow();
            window.DataContext = viewModel;

            MainWindow = window;
            window.Show();
        }
    }
}
