using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StudentsSqlite.Data;
using StudentsSqlite.Models;

namespace StudentsSqlite.Services
{
    /// <summary>
    /// Контракт хранилища записей. Модель представления работает только
    /// с этим интерфейсом, поэтому она не зависит от того, где именно
    /// хранятся данные — в базе SQLite, в XML-файле или на сервере.
    /// </summary>
    public interface IStudentRepository
    {
        string StorageLocation { get; }

        void EnsureCreated();

        List<Student> GetAll();

        void Add(Student student);

        void Update(Student student);

        void Delete(Student student);
    }

    /// <summary>
    /// Хранилище записей в базе данных SQLite через Entity Framework Core.
    /// Для каждой операции создаётся отдельный экземпляр контекста: такой подход
    /// рекомендован разработчиками EF Core, так как контекст является
    /// «единицей работы» и не должен жить всё время работы приложения.
    /// </summary>
    public sealed class EfStudentRepository : IStudentRepository
    {
        private readonly string _databasePath;

        public EfStudentRepository(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Не задан путь к файлу базы данных", "databasePath");
            }

            _databasePath = databasePath;
        }

        public string StorageLocation
        {
            get { return _databasePath; }
        }

        /// <summary>
        /// Создаёт файл базы данных и таблицу Students, если их ещё нет.
        /// Вызывается один раз при запуске приложения.
        /// </summary>
        public void EnsureCreated()
        {
            using (AppDbContext context = CreateContext())
            {
                context.Database.EnsureCreated();
            }
        }

        /// <summary>Читает все записи. AsNoTracking ускоряет выборку только для чтения.</summary>
        public List<Student> GetAll()
        {
            using (AppDbContext context = CreateContext())
            {
                return context.Students
                              .AsNoTracking()
                              .OrderBy(student => student.LastName)
                              .ThenBy(student => student.FirstName)
                              .ToList();
            }
        }

        /// <summary>
        /// Добавляет запись. После вызова SaveChanges свойство Id заполняется
        /// значением, которое сгенерировала база данных.
        /// </summary>
        public void Add(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            using (AppDbContext context = CreateContext())
            {
                context.Students.Add(student);
                context.SaveChanges();
            }
        }

        public void Update(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            using (AppDbContext context = CreateContext())
            {
                context.Students.Update(student);
                context.SaveChanges();
            }
        }

        public void Delete(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            using (AppDbContext context = CreateContext())
            {
                Student entity = context.Students.Find(student.Id);

                if (entity == null)
                {
                    return;
                }

                context.Students.Remove(entity);
                context.SaveChanges();
            }
        }

        private AppDbContext CreateContext()
        {
            return new AppDbContext(_databasePath);
        }
    }
}
