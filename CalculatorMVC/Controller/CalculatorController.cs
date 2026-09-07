using System;
using CalculatorMVC.Model;
using CalculatorMVC.View;

namespace CalculatorMVC.Controller
{
    /// <summary>
    /// Контроллер (слой Controller в схеме MVC).
    /// Принимает события представления, преобразует их в операции над моделью
    /// и обновляет представление новыми данными модели.
    /// </summary>
    public sealed class CalculatorController
    {
        private readonly CalculatorModel _model;
        private readonly IMainView _view;

        public CalculatorController(CalculatorModel model, IMainView view)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            _model = model;
            _view = view;
        }

        /// <summary>Подписка на события представления и вывод начального состояния.</summary>
        public void Initialize()
        {
            _view.CommandEntered += OnCommandEntered;
            _view.ExpressionEdited += OnExpressionEdited;
            _view.AngleModeChanged += OnAngleModeChanged;

            _view.UseDegrees = _model.UseDegrees;
            UpdateView();
        }

        private void OnExpressionEdited(object sender, string text)
        {
            _model.SetExpression(text);
        }

        private void OnAngleModeChanged(object sender, EventArgs e)
        {
            _model.UseDegrees = _view.UseDegrees;
        }

        private void OnCommandEntered(object sender, string command)
        {
            try
            {
                switch (command)
                {
                    case "#eval":
                        Evaluate();
                        break;

                    case "#clear":
                        _model.ClearAll();
                        _view.ResultText = "0";
                        break;

                    case "#clearentry":
                        _model.ClearEntry();
                        break;

                    case "#back":
                        _model.Backspace();
                        break;

                    case "#sign":
                        _model.ToggleSign();
                        break;

                    case "#ans":
                        _model.Append(CalculatorModel.Format(_model.LastResult));
                        break;

                    case "#mplus":
                        _model.MemoryAdd(GetCurrentValue());
                        break;

                    case "#mminus":
                        _model.MemorySubtract(GetCurrentValue());
                        break;

                    case "#mr":
                        _model.Append(CalculatorModel.Format(_model.Memory));
                        break;

                    case "#mc":
                        _model.MemoryClear();
                        break;

                    case "#refresh":
                        _model.SetExpression(_view.ExpressionText);
                        break;

                    default:
                        _model.Append(command);
                        break;
                }

                UpdateView();
            }
            catch (ExpressionException exception)
            {
                UpdateView();
                _view.ShowError(exception.Message);
            }
            catch (Exception exception)
            {
                UpdateView();
                _view.ShowError(exception.Message);
            }
        }

        private void Evaluate()
        {
            double result = _model.Evaluate();
            _view.ResultText = "= " + CalculatorModel.Format(result);
            _model.SetExpression(CalculatorModel.Format(result));
        }

        /// <summary>
        /// Значение для операций с памятью: результат текущего выражения,
        /// а если выражение пустое — последний полученный результат.
        /// </summary>
        private double GetCurrentValue()
        {
            if (string.IsNullOrWhiteSpace(_model.Expression))
            {
                return _model.LastResult;
            }

            return _model.Evaluate();
        }

        private void UpdateView()
        {
            _view.ExpressionText = _model.Expression;
            _view.ShowHistory(_model.History);
            _view.ShowMemoryState(_model.HasMemory, CalculatorModel.Format(_model.Memory));
        }
    }
}
