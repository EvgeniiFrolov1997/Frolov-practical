using System.Windows;

namespace StudentsXml.Views
{
    /// <summary>
    /// Модальное окно добавления и редактирования записи.
    /// Открывается методом ShowDialog, поэтому главное окно блокируется
    /// до закрытия диалога. Результат работы окна (ОК или Отмена)
    /// устанавливается сервисом диалогов по событию CloseRequested.
    /// </summary>
    public partial class StudentEditWindow : Window
    {
        public StudentEditWindow()
        {
            InitializeComponent();
        }
    }
}
