using System;
using System.ComponentModel;
using System.Windows.Input;
using StudentsXml.Models;

namespace StudentsXml.ViewModels
{
    /// <summary>
    /// Модель представления диалогового окна добавления/редактирования.
    /// Реализует IDataErrorInfo — стандартный механизм проверки данных в WPF:
    /// поле с ошибкой подсвечивается, а кнопка «ОК» остаётся недоступной,
    /// пока все поля не заполнены корректно.
    /// </summary>
    public sealed class StudentEditViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly Student _student;

        private string _firstName;
        private string _lastName;
        private string _middleName;
        private int _age;

        public StudentEditViewModel(Student student, string title)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            _student = student;
            Title = title;

            _firstName = student.FirstName;
            _lastName = student.LastName;
            _middleName = student.MiddleName;
            _age = student.Age;

            OkCommand = new RelayCommand(ExecuteOk, CanExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>Заголовок окна: «Добавить студента» или «Изменить студента».</summary>
        public string Title { get; private set; }

        public string FirstName
        {
            get { return _firstName; }
            set { SetProperty(ref _firstName, value); }
        }

        public string LastName
        {
            get { return _lastName; }
            set { SetProperty(ref _lastName, value); }
        }

        public string MiddleName
        {
            get { return _middleName; }
            set { SetProperty(ref _middleName, value); }
        }

        public int Age
        {
            get { return _age; }
            set { SetProperty(ref _age, value); }
        }

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        /// <summary>Запрос на закрытие окна: true — «ОК», false — «Отмена».</summary>
        public event EventHandler<bool> CloseRequested;

        private bool CanExecuteOk(object parameter)
        {
            return string.IsNullOrEmpty(Error);
        }

        private void ExecuteOk(object parameter)
        {
            // Данные переносятся в модель только при подтверждении
            _student.FirstName = (FirstName ?? string.Empty).Trim();
            _student.LastName = (LastName ?? string.Empty).Trim();
            _student.MiddleName = (MiddleName ?? string.Empty).Trim();
            _student.Age = Age;

            RaiseCloseRequested(true);
        }

        private void ExecuteCancel(object parameter)
        {
            RaiseCloseRequested(false);
        }

        private void RaiseCloseRequested(bool result)
        {
            EventHandler<bool> handler = CloseRequested;
            if (handler != null)
            {
                handler(this, result);
            }
        }

        #region Проверка вводимых данных

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case "FirstName":
                        if (string.IsNullOrWhiteSpace(FirstName))
                        {
                            return "Имя обязательно для заполнения";
                        }

                        if (FirstName.Trim().Length < 2)
                        {
                            return "Имя слишком короткое";
                        }

                        break;

                    case "LastName":
                        if (string.IsNullOrWhiteSpace(LastName))
                        {
                            return "Фамилия обязательна для заполнения";
                        }

                        if (LastName.Trim().Length < 2)
                        {
                            return "Фамилия слишком короткая";
                        }

                        break;

                    case "Age":
                        if (Age < 14 || Age > 100)
                        {
                            return "Возраст должен быть в диапазоне от 14 до 100 лет";
                        }

                        break;
                }

                return null;
            }
        }

        /// <summary>Общая ошибка формы: непустая строка, если некорректно хотя бы одно поле.</summary>
        public string Error
        {
            get
            {
                string[] properties = { "FirstName", "LastName", "Age" };

                foreach (string property in properties)
                {
                    string error = this[property];
                    if (!string.IsNullOrEmpty(error))
                    {
                        return error;
                    }
                }

                return null;
            }
        }

        #endregion
    }
}
