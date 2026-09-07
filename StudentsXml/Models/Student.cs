using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace StudentsXml.Models
{
    /// <summary>
    /// Модель данных «Студент».
    /// Реализует INotifyPropertyChanged, поэтому изменение любого свойства
    /// автоматически отражается в DataGrid без ручного обновления таблицы.
    /// Класс помечен атрибутами XML-сериализации: именно в таком виде
    /// записи сохраняются в файл students.xml.
    /// </summary>
    [Serializable]
    [XmlType("Student")]
    public sealed class Student : INotifyPropertyChanged
    {
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _middleName = string.Empty;
        private int _age;

        [XmlElement("FirstName")]
        public string FirstName
        {
            get { return _firstName; }
            set { SetField(ref _firstName, value); }
        }

        [XmlElement("LastName")]
        public string LastName
        {
            get { return _lastName; }
            set { SetField(ref _lastName, value); }
        }

        [XmlElement("MiddleName")]
        public string MiddleName
        {
            get { return _middleName; }
            set { SetField(ref _middleName, value); }
        }

        [XmlElement("Age")]
        public int Age
        {
            get { return _age; }
            set { SetField(ref _age, value); }
        }

        /// <summary>Полное имя — вычисляемое свойство, в XML не сохраняется.</summary>
        [XmlIgnore]
        public string FullName
        {
            get { return (LastName + " " + FirstName + " " + MiddleName).Trim(); }
        }

        /// <summary>Создаёт независимую копию объекта (для редактирования в диалоге).</summary>
        public Student Clone()
        {
            return new Student
            {
                FirstName = FirstName,
                LastName = LastName,
                MiddleName = MiddleName,
                Age = Age
            };
        }

        /// <summary>Копирует значения из другого объекта в текущий.</summary>
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
