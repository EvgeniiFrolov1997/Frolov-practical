using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalculatorMVC.Model
{
    /// <summary>
    /// Модель калькулятора (слой Model в схеме MVC).
    /// Хранит состояние: текущее выражение, регистр памяти, последний результат
    /// и историю вычислений. О пользовательском интерфейсе ничего не знает.
    /// </summary>
    public sealed class CalculatorModel
    {
        private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();
        private readonly List<string> _history = new List<string>();

        public CalculatorModel()
        {
            Expression = string.Empty;
        }

        /// <summary>Текущее введённое выражение.</summary>
        public string Expression { get; private set; }

        /// <summary>Значение регистра памяти.</summary>
        public double Memory { get; private set; }

        /// <summary>Признак того, что в памяти сохранено значение.</summary>
        public bool HasMemory { get; private set; }

        /// <summary>Последний вычисленный результат (кнопка Ans).</summary>
        public double LastResult { get; private set; }

        /// <summary>История вычислений (только для чтения).</summary>
        public IList<string> History
        {
            get { return _history.AsReadOnly(); }
        }

        /// <summary>Режим измерения углов: градусы или радианы.</summary>
        public bool UseDegrees
        {
            get { return _evaluator.UseDegrees; }
            set { _evaluator.UseDegrees = value; }
        }

        public void Append(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Expression += text;
            }
        }

        public void SetExpression(string text)
        {
            Expression = text ?? string.Empty;
        }

        public void Backspace()
        {
            if (Expression.Length > 0)
            {
                Expression = Expression.Substring(0, Expression.Length - 1);
            }
        }

        public void ClearEntry()
        {
            Expression = string.Empty;
        }

        public void ClearAll()
        {
            Expression = string.Empty;
            LastResult = 0;
        }

        /// <summary>Смена знака всего выражения.</summary>
        public void ToggleSign()
        {
            if (Expression.Length == 0)
            {
                Expression = "-";
            }
            else if (Expression.StartsWith("-", StringComparison.Ordinal))
            {
                Expression = Expression.Substring(1);
            }
            else
            {
                Expression = "-" + Expression;
            }
        }

        /// <summary>Вычисляет текущее выражение и записывает результат в историю.</summary>
        public double Evaluate()
        {
            double result = _evaluator.Evaluate(Expression);
            LastResult = result;
            _history.Insert(0, Expression + " = " + Format(result));

            if (_history.Count > 100)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            return result;
        }

        public void MemoryAdd(double value)
        {
            Memory += value;
            HasMemory = true;
        }

        public void MemorySubtract(double value)
        {
            Memory -= value;
            HasMemory = true;
        }

        public void MemoryClear()
        {
            Memory = 0;
            HasMemory = false;
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>Единое форматирование числа (десятичный разделитель — точка).</summary>
        public static string Format(double value)
        {
            if (Math.Abs(value) < 1e-12)
            {
                return "0";
            }

            return value.ToString("G12", CultureInfo.InvariantCulture);
        }
    }
}
