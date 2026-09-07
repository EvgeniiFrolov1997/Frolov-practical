using System;
using System.Windows.Forms;
using CalculatorMVC.Controller;
using CalculatorMVC.Model;
using CalculatorMVC.View;

namespace CalculatorMVC
{
    /// <summary>
    /// Точка входа приложения. Здесь собирается связка Model - View - Controller:
    /// создаётся модель, создаётся представление и создаётся контроллер,
    /// который подписывается на события представления и управляет моделью.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CalculatorModel model = new CalculatorModel();
            MainForm view = new MainForm();
            CalculatorController controller = new CalculatorController(model, view);
            controller.Initialize();

            Application.Run(view);
        }
    }
}
