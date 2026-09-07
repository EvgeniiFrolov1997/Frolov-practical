using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudentsSqlite.Models
{
    /// <summary>
    /// Сущность «Студент». Каждый объект этого класса соответствует
    /// одной строке таблицы Students в базе данных SQLite.
    /// Свойство Id является первичным ключом и заполняется базой данных автоматически.
    /// Реализация INotifyPropertyChanged обеспечивает обновление DataGrid
    /// после редактирования записи.
    /// </summary>
    public sealed class Student : INotifyPropertyChanged
    {
        private int _id;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _middleName = string.Empty;
        private int _age;

        /// <summary>Первичный ключ (INTEGER PRIMARY KEY AUTOINCREMENT).</summary>
        public int Id
        {
            get { return _id; }
            set { SetField(ref _id, value); }
        }

        public string FirstName
        {
            get { return _firstName; }
            set { SetField(ref _firstName, value); }
        }

        public string LastName
        {
            get { return _lastName; }
            set { SetField(ref _lastName, value); }
        }

        public string MiddleName
        {
            get { return _middleName; }
            set { SetField(ref _middleName, value); }
        }

        public int Age
        {
            get { return _age; }
            set { SetField(ref _age, value); }
        }

        /// <summary>Полное имя. В базе данных не хранится (помечено Ignore в конфигурации).</summary>
        public string FullName
        {
            get { return (LastName + " " + FirstName + " " + MiddleName).Trim(); }
        }

        /// <summary>Копия объекта для редактирования в диалоговом окне.</summary>
        public Student Clone()
        {
            return new Student
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                MiddleName = MiddleName,
                Age = Age
            };
        }

        /// <summary>Перенос значений из отредактированной копии в исходный объект.</summary>
        public void CopyFrom(Student source)
        {
            if (source == null)
            {
                return;
            }

            FirstName = source.FirstName;
            LastName = source.LastName;
            MiddleName = source.MiddleName;
            Age = source.Age;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
                handler(this, new PropertyChangedEventArgs("FullName"));
            }
        }
    }
}
