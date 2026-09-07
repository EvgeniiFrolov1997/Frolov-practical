using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StudentsSqlite.Models;
using StudentsSqlite.Services;

namespace StudentsSqlite.ViewModels
{
    /// <summary>
    /// Модель представления главного окна.
    /// Содержит коллекцию записей, выделенную запись и три команды: добавить,
    /// изменить, удалить. Все операции выполняются над базой данных SQLite
    /// через интерфейс IStudentRepository; о существовании окон ViewModel не знает.
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
            RefreshCommand = new RelayCommand(parameter => LoadData());

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

        /// <summary>Путь к файлу базы данных для вывода в строке состояния.</summary>
        public string StorageLocation
        {
            get { return _repository.StorageLocation; }
        }

        public ICommand AddCommand { get; private set; }

        public ICommand EditCommand { get; private set; }

        public ICommand DeleteCommand { get; private set; }

        public ICommand RefreshCommand { get; private set; }

        /// <summary>Загрузка записей из базы данных (выполняется при запуске приложения).</summary>
        private void LoadData()
        {
            try
            {
                List<Student> students = _repository.GetAll();

                Students.Clear();
                foreach (Student student in students)
                {
                    Students.Add(student);
                }

                SelectedStudent = null;

                StatusMessage = Students.Count == 0
                    ? "База данных пуста"
                    : "Загружено записей: " + Students.Count;
            }
            catch (Exception exception)
            {
                _dialogService.ShowError("Не удалось прочитать базу данных: " + exception.Message,
                                         "Ошибка загрузки");
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

            try
            {
                // После сохранения EF Core заполняет свойство Id значением из базы
                _repository.Add(student);

                Students.Add(student);
                SelectedStudent = student;

                StatusMessage = "Запись добавлена (Id = " + student.Id + "). Всего записей: " + Students.Count;
            }
            catch (Exception exception)
            {
                ReportSaveError(exception);
            }
        }

        private void ExecuteEdit(object parameter)
        {
            Student original = SelectedStudent;
            if (original == null)
            {
                return;
            }

            // Редактируется копия: при отмене исходная запись не изменится
            Student copy = original.Clone();

            if (!_dialogService.EditStudent(copy, "Изменить студента"))
            {
                return;
            }

            try
            {
                _repository.Update(copy);
                original.CopyFrom(copy);

                StatusMessage = "Запись изменена (Id = " + original.Id + ")";
            }
            catch (Exception exception)
            {
                ReportSaveError(exception);
            }
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

            try
            {
                _repository.Delete(student);

                Students.Remove(student);
                SelectedStudent = null;

                StatusMessage = "Запись удалена. Всего записей: " + Students.Count;
            }
            catch (Exception exception)
            {
                ReportSaveError(exception);
            }
        }

        private void ReportSaveError(Exception exception)
        {
            _dialogService.ShowError("Не удалось сохранить изменения в базе данных: " + exception.Message,
                                     "Ошибка сохранения");
            StatusMessage = "Ошибка сохранения";
        }
    }
}
