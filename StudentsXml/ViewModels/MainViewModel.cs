using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StudentsXml.Models;
using StudentsXml.Services;

namespace StudentsXml.ViewModels
{
    /// <summary>
    /// Модель представления главного окна.
    /// Содержит коллекцию записей, выделенную запись и три команды: добавить,
    /// изменить, удалить. О существовании конкретных окон не знает —
    /// диалоги открываются через IDialogService.
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly IStudentRepository _repository;
        private readonly IDialogService _dialogService;

        private Student _selectedStudent;
        private string _statusMessage;

        public MainViewModel(IStudentRepository repository, IDialogService dialogService)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (dialogService == null)
            {
                throw new ArgumentNullException("dialogService");
            }

            _repository = repository;
            _dialogService = dialogService;

            Students = new ObservableCollection<Student>();

            AddCommand = new RelayCommand(ExecuteAdd);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteWithSelection);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteWithSelection);

            LoadData();
        }

        /// <summary>Записи, отображаемые в DataGrid.</summary>
        public ObservableCollection<Student> Students { get; private set; }

        /// <summary>Выделенная в таблице строка (null, если ничего не выделено).</summary>
        public Student SelectedStudent
        {
            get { return _selectedStudent; }
            set { SetProperty(ref _selectedStudent, value); }
        }

        /// <summary>Текст строки состояния.</summary>
        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty(ref _statusMessage, value); }
        }

        public string StorageLocation
        {
            get { return _repository.StorageLocation; }
        }

        public ICommand AddCommand { get; private set; }

        public ICommand EditCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }

        /// <summary>Загрузка данных из XML-файла при запуске приложения.</summary>
        private void LoadData()
        {
            try
            {
                List<Student> students = _repository.Load();

                Students.Clear();
                foreach (Student student in students)
                {
                    Students.Add(student);
                }

                StatusMessage = Students.Count == 0
                    ? "Файл данных пуст или ещё не создан"
                    : "Загружено записей: " + Students.Count;
            }
            catch (Exception exception)
            {
                _dialogService.ShowError("Не удалось загрузить данные: " + exception.Message, "Ошибка загрузки");
                StatusMessage = "Данные не загружены";
            }
        }

        private bool CanExecuteWithSelection(object parameter)
        {
            return SelectedStudent != null;
        }

        private void ExecuteAdd(object parameter)
        {
            Student student = new Student();

            if (!_dialogService.EditStudent(student, "Добавить студента"))
            {
                return;
            }

            Students.Add(student);
            SelectedStudent = student;
            Save("Запись добавлена");
        }

        private void ExecuteEdit(object parameter)
        {
            Student original = SelectedStudent;
            if (original == null)
            {
                return;
            }

            // Редактируется копия: при отмене исходные данные не пострадают
            Student copy = original.Clone();

            if (!_dialogService.EditStudent(copy, "Изменить студента"))
            {
                return;
            }

            original.CopyFrom(copy);
            Save("Запись изменена");
        }

        private void ExecuteDelete(object parameter)
        {
            Student student = SelectedStudent;
            if (student == null)
            {
                return;
            }

            string question = "Удалить запись «" + student.FullName + "»?";
            if (!_dialogService.Confirm(question, "Подтверждение удаления"))
            {
                return;
            }

            Students.Remove(student);
            SelectedStudent = null;
            Save("Запись удалена");
        }

        /// <summary>Сохранение всей коллекции в XML после каждой операции.</summary>
        private void Save(string message)
        {
            try
            {
                _repository.Save(Students);
                StatusMessage = message + ". Всего записей: " + Students.Count;
            }
            catch (Exception exception)
            {
                _dialogService.ShowError("Не удалось сохранить данные: " + exception.Message, "Ошибка сохранения");
                StatusMessage = "Ошибка сохранения";
            }
        }
    }
}
