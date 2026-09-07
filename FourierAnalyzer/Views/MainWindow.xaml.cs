using System.Windows;

namespace FourierAnalyzer.Views
{
    /// <summary>
    /// Главное окно приложения. Согласно шаблону MVVM прикладного кода
    /// в файле фоновой логики нет: вся логика находится в MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
