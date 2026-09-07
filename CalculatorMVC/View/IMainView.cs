using System;
using System.Collections.Generic;

namespace CalculatorMVC.View
{
    /// <summary>
    /// Контракт представления. Контроллер работает только с этим интерфейсом
    /// и ничего не знает о конкретной реализации на Windows Forms,
    /// поэтому представление можно заменить (например, на WPF) без правки контроллера.
    /// </summary>
    public interface IMainView
    {
        /// <summary>Текст в поле ввода выражения.</summary>
        string ExpressionText { get; set; }

        /// <summary>Текст в поле результата.</summary>
        string ResultText { get; set; }

        /// <summary>Выбранный пользователем режим измерения углов.</summary>
        bool UseDegrees { get; set; }

        /// <summary>Нажата кнопка панели: передаётся код команды.</summary>
        event EventHandler<string> CommandEntered;

        /// <summary>Пользователь изменил выражение с клавиатуры.</summary>
        event EventHandler<string> ExpressionEdited;

        /// <summary>Переключён режим «градусы / радианы».</summary>
        event EventHandler AngleModeChanged;

        void ShowHistory(IList<string> records);

        void ShowMemoryState(bool hasMemory, string memoryValue);

        void ShowError(string message);
    }
}
