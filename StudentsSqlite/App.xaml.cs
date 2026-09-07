using System;
using System.IO;
using System.Windows;
using StudentsSqlite.Services;
using StudentsSqlite.ViewModels;

namespace StudentsSqlite
{
    /// <summary>
    /// Точка входа приложения. Здесь создаются хранилище данных, сервис диалогов
    /// и модель представления, а также выполняется создание файла базы данных,
    /// если он ещё не существует.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Имя файла базы данных SQLite, создаваемого программой.</summary>
        private const string DatabaseFileName = "students.db";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFileName);

            IStudentRepository repository = new EfStudentRepository(databasePath);
            IDialogService dialogService = new DialogService();

            try
            {
                // Создаёт файл базы данных и таблицу Students при первом запуске
                repository.EnsureCreated();
            }
            catch (Exception exception)
            {
                dialogService.ShowError("Не удалось создать базу данных: " + exception.Message,
                                        "Критическая ошибка");
                Shutdown();
                return;
            }

            MainViewModel viewModel = new MainViewModel(repository, dialogService);

            Views.MainWindow window = new Views.MainWindow();
            window.DataContext = viewModel;

            MainWindow = window;
            window.Show();
        }
    }
}
