using System.Windows;

namespace StudentsXml.Views
{
    /// <summary>
    /// Главное окно приложения.
    /// Согласно шаблону MVVM файл фоновой логики не содержит прикладного кода:
    /// вся логика находится в MainViewModel, связь выполняется привязками XAML.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
