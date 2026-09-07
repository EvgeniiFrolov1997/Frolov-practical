using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using StudentsXml.Models;

namespace StudentsXml.Services
{
    /// <summary>
    /// Контракт хранилища записей. ViewModel работает только с интерфейсом,
    /// поэтому XML-хранилище можно заменить на базу данных, не меняя логику приложения.
    /// </summary>
    public interface IStudentRepository
    {
        string StorageLocation { get; }

        List<Student> Load();

        void Save(IEnumerable<Student> students);
    }

    /// <summary>
    /// Хранилище записей в XML-файле.
    /// Файл создаётся автоматически при первом сохранении; если файл существует,
    /// его содержимое загружается при старте приложения.
    /// </summary>
    public sealed class XmlStudentRepository : IStudentRepository
    {
        private readonly string _filePath;
        private readonly XmlSerializer _serializer;

        public XmlStudentRepository(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Не задан путь к файлу данных", "filePath");
            }

            _filePath = filePath;

            // Корневой элемент документа называется <Students>
            _serializer = new XmlSerializer(typeof(List<Student>), new XmlRootAttribute("Students"));
        }

        public string StorageLocation
        {
            get { return _filePath; }
        }

        public List<Student> Load()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Student>();
            }

            try
            {
                using (FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                {
                    List<Student> students = _serializer.Deserialize(stream) as List<Student>;
                    return students ?? new List<Student>();
                }
            }
            catch (InvalidOperationException exception)
            {
                throw new IOException("Файл данных повреждён или имеет неверный формат: " + _filePath, exception);
            }
        }

        public void Save(IEnumerable<Student> students)
        {
            if (students == null)
            {
                throw new ArgumentNullException("students");
            }

            string directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false)
            };

            // Убираем служебные пространства имён xsi и xsd из результирующего файла
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using (XmlWriter writer = XmlWriter.Create(_filePath, settings))
            {
                _serializer.Serialize(writer, new List<Student>(students), namespaces);
            }
        }
    }
}
