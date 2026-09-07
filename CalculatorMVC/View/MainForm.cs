using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CalculatorMVC.View
{
    /// <summary>
    /// Главное окно приложения (слой View в схеме MVC).
    /// Отвечает только за отображение данных и за передачу действий пользователя
    /// контроллеру через события. Вычислений не выполняет.
    /// </summary>
    public sealed class MainForm : Form, IMainView
    {
        private readonly TextBox _txtExpression = new TextBox();
        private readonly Label _lblResult = new Label();
        private readonly Label _lblMemory = new Label();
        private readonly ListBox _lstHistory = new ListBox();
        private readonly CheckBox _chkDegrees = new CheckBox();

        private bool _suppressTextChanged;

        private static readonly Color DigitColor = Color.White;
        private static readonly Color OperationColor = Color.FromArgb(238, 238, 238);
        private static readonly Color FunctionColor = Color.FromArgb(222, 235, 247);
        private static readonly Color ServiceColor = Color.FromArgb(252, 228, 214);
        private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);

        public event EventHandler<string> CommandEntered;
        public event EventHandler<string> ExpressionEdited;
        public event EventHandler AngleModeChanged;

        public MainForm()
        {
            InitializeInterface();
        }

        #region Реализация IMainView

        public string ExpressionText
        {
            get { return _txtExpression.Text; }
            set
            {
                _suppressTextChanged = true;
                _txtExpression.Text = value;
                _txtExpression.SelectionStart = _txtExpression.TextLength;
                _txtExpression.SelectionLength = 0;
                _suppressTextChanged = false;
            }
        }

        public string ResultText
        {
            get { return _lblResult.Text; }
            set { _lblResult.Text = value; }
        }

        public bool UseDegrees
        {
            get { return _chkDegrees.Checked; }
            set { _chkDegrees.Checked = value; }
        }

        public void ShowHistory(IList<string> records)
        {
            _lstHistory.BeginUpdate();
            _lstHistory.Items.Clear();

            foreach (string record in records)
            {
                _lstHistory.Items.Add(record);
            }

            _lstHistory.EndUpdate();
        }

        public void ShowMemoryState(bool hasMemory, string memoryValue)
        {
            _lblMemory.Text = hasMemory ? "M = " + memoryValue : string.Empty;
        }

        public void ShowError(string message)
        {
            _lblResult.ForeColor = Color.Firebrick;
            _lblResult.Text = "Ошибка: " + message;
        }

        #endregion

        #region Построение интерфейса

        private void InitializeInterface()
        {
            Text = "Калькулятор (MVC) — Фролов Евгений, группа К-ИСП-21";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 620);
            MinimumSize = new Size(760, 560);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(250, 250, 250);
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            Controls.Add(CreateKeypad());
            Controls.Add(CreateHistoryPanel());
            Controls.Add(CreateDisplayPanel());
        }

        private Control CreateDisplayPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 122,
                Padding = new Padding(10, 8, 10, 4),
                BackColor = Color.White
            };

            FlowLayoutPanel options = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight
            };

            _chkDegrees.Text = "Градусы (снять — радианы)";
            _chkDegrees.Checked = true;
            _chkDegrees.AutoSize = true;
            _chkDegrees.CheckedChanged += delegate
            {
                EventHandler handler = AngleModeChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            _lblMemory.AutoSize = true;
            _lblMemory.ForeColor = AccentColor;
            _lblMemory.Padding = new Padding(20, 4, 0, 0);

            options.Controls.Add(_chkDegrees);
            options.Controls.Add(_lblMemory);

            _lblResult.Dock = DockStyle.Top;
            _lblResult.Height = 34;
            _lblResult.TextAlign = ContentAlignment.MiddleRight;
            _lblResult.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            _lblResult.ForeColor = AccentColor;
            _lblResult.Text = "0";

            _txtExpression.Dock = DockStyle.Top;
            _txtExpression.Font = new Font("Consolas", 16F);
            _txtExpression.TextAlign = HorizontalAlignment.Right;
            _txtExpression.BorderStyle = BorderStyle.FixedSingle;
            _txtExpression.TextChanged += TxtExpression_TextChanged;

            panel.Controls.Add(options);
            panel.Controls.Add(_lblResult);
            panel.Controls.Add(_txtExpression);

            return panel;
        }

        private Control CreateHistoryPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(250, 250, 250)
            };

            _lstHistory.Dock = DockStyle.Fill;
            _lstHistory.Font = new Font("Consolas", 9F);
            _lstHistory.BorderStyle = BorderStyle.FixedSingle;
            _lstHistory.DoubleClick += LstHistory_DoubleClick;

            Label caption = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Text = "История (двойной щелчок — вставить)",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray
            };

            panel.Controls.Add(_lstHistory);
            panel.Controls.Add(caption);

            return panel;
        }

        private Control CreateKeypad()
        {
            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 8,
                Padding = new Padding(6)
            };

            for (int column = 0; column < table.ColumnCount; column++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / table.ColumnCount));
            }

            for (int row = 0; row < table.RowCount; row++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / table.RowCount));
            }

            // Строка 0 — служебные команды и память
            AddButton(table, 0, 0, "C", "#clear", ServiceColor);
            AddButton(table, 1, 0, "CE", "#clearentry", ServiceColor);
            AddButton(table, 2, 0, "\u2190", "#back", ServiceColor);
            AddButton(table, 3, 0, "MC", "#mc", ServiceColor);
            AddButton(table, 4, 0, "MR", "#mr", ServiceColor);
            AddButton(table, 5, 0, "M+", "#mplus", ServiceColor);

            // Строка 1 — память, константы, тригонометрия
            AddButton(table, 0, 1, "M-", "#mminus", ServiceColor);
            AddButton(table, 1, 1, "\u03c0", "pi", FunctionColor);
            AddButton(table, 2, 1, "e", "e", FunctionColor);
            AddButton(table, 3, 1, "sin", "sin(", FunctionColor);
            AddButton(table, 4, 1, "cos", "cos(", FunctionColor);
            AddButton(table, 5, 1, "tan", "tan(", FunctionColor);

            // Строка 2 — обратные тригонометрические и логарифмы
            AddButton(table, 0, 2, "ctg", "ctg(", FunctionColor);
            AddButton(table, 1, 2, "asin", "asin(", FunctionColor);
            AddButton(table, 2, 2, "acos", "acos(", FunctionColor);
            AddButton(table, 3, 2, "atan", "atan(", FunctionColor);
            AddButton(table, 4, 2, "ln", "ln(", FunctionColor);
            AddButton(table, 5, 2, "log", "log(", FunctionColor);

            // Строка 3 — степени, корень, факториал
            AddButton(table, 0, 3, "\u221a", "sqrt(", FunctionColor);
            AddButton(table, 1, 3, "x^y", "^", FunctionColor);
            AddButton(table, 2, 3, "n!", "!", FunctionColor);
            AddButton(table, 3, 3, "1/x", "1/(", FunctionColor);
            AddButton(table, 4, 3, "e^x", "exp(", FunctionColor);
            AddButton(table, 5, 3, "|x|", "abs(", FunctionColor);

            // Строки 4-6 — цифры и арифметические операции
            AddButton(table, 0, 4, "7", "7", DigitColor);
            AddButton(table, 1, 4, "8", "8", DigitColor);
            AddButton(table, 2, 4, "9", "9", DigitColor);
            AddButton(table, 3, 4, "/", "/", OperationColor);
            AddButton(table, 4, 4, "(", "(", OperationColor);
            AddButton(table, 5, 4, ")", ")", OperationColor);

            AddButton(table, 0, 5, "4", "4", DigitColor);
            AddButton(table, 1, 5, "5", "5", DigitColor);
            AddButton(table, 2, 5, "6", "6", DigitColor);
            AddButton(table, 3, 5, "*", "*", OperationColor);
            AddButton(table, 4, 5, "mod", "%", OperationColor);
            AddButton(table, 5, 5, "\u00b1", "#sign", OperationColor);

            AddButton(table, 0, 6, "1", "1", DigitColor);
            AddButton(table, 1, 6, "2", "2", DigitColor);
            AddButton(table, 2, 6, "3", "3", DigitColor);
            AddButton(table, 3, 6, "-", "-", OperationColor);
            AddButton(table, 4, 6, "+", "+", OperationColor);

            Button equals = AddButton(table, 5, 6, "=", "#eval", AccentColor);
            equals.ForeColor = Color.White;
            equals.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            table.SetRowSpan(equals, 2);

            // Строка 7 — ноль, разделитель, дополнительные операции
            Button zero = AddButton(table, 0, 7, "0", "0", DigitColor);
            table.SetColumnSpan(zero, 3);
            AddButton(table, 3, 7, ".", ".", DigitColor);
            AddButton(table, 4, 7, "Ans", "#ans", OperationColor);

            return table;
        }

        private Button AddButton(TableLayoutPanel table, int column, int row, string caption, string command, Color color)
        {
            Button button = new Button
            {
                Text = caption,
                Tag = command,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F),
                TabStop = false
            };

            button.FlatAppearance.BorderColor = Color.Silver;
            button.Click += Button_Click;

            table.Controls.Add(button, column, row);
            return button;
        }

        #endregion

        #region Обработчики событий представления

        private void Button_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
            {
                return;
            }

            _lblResult.ForeColor = AccentColor;
            RaiseCommand((string)button.Tag);
            _txtExpression.Focus();
            _txtExpression.SelectionStart = _txtExpression.TextLength;
        }

        private void TxtExpression_TextChanged(object sender, EventArgs e)
        {
            if (_suppressTextChanged)
            {
                return;
            }

            EventHandler<string> handler = ExpressionEdited;
            if (handler != null)
            {
                handler(this, _txtExpression.Text);
            }
        }

        private void LstHistory_DoubleClick(object sender, EventArgs e)
        {
            if (_lstHistory.SelectedItem == null)
            {
                return;
            }

            string record = _lstHistory.SelectedItem.ToString();
            int index = record.LastIndexOf(" = ", StringComparison.Ordinal);

            if (index > 0)
            {
                ExpressionText = record.Substring(0, index);
                RaiseCommand("#refresh");
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RaiseCommand("#eval");
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                RaiseCommand("#clear");
            }
        }

        private void RaiseCommand(string command)
        {
            EventHandler<string> handler = CommandEntered;
            if (handler != null)
            {
                handler(this, command);
            }
        }

        #endregion
    }
}
