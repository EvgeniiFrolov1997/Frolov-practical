using System.Windows;
using StudentsXml.Models;
using StudentsXml.ViewModels;
using StudentsXml.Views;

namespace StudentsXml.Services
{
    /// <summary>
    /// Контракт сервиса диалогов. Благодаря ему ViewModel не создаёт окна
    /// напрямую и не ссылается на пространство имён Views — требование шаблона MVVM.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Показывает модальное окно редактирования. Возвращает true, если нажата кнопка «ОК».</summary>
        bool EditStudent(Student student, string title);

        bool Confirm(string message, string caption);

        void ShowError(string message, string caption);
    }

    /// <summary>Реализация сервиса диалогов средствами WPF.</summary>
    public sealed class DialogService : IDialogService
    {
        public bool EditStudent(Student student, string title)
        {
            StudentEditViewModel viewModel = new StudentEditViewModel(student, title);

            StudentEditWindow window = new StudentEditWindow
            {
                DataContext = viewModel,
                Owner = Application.Current != null ? Application.Current.MainWindow : null
            };

            viewModel.CloseRequested += (sender, result) => window.DialogResult = result;

            // ShowDialog открывает окно в модальном режиме:
            // главное окно блокируется до закрытия диалога
            return window.ShowDialog() == true;
        }

        public bool Confirm(string message, string caption)
        {
            MessageBoxResult result = MessageBox.Show(
                message,
                caption,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        public void ShowError(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
