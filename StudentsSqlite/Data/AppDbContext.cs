using System;
using Microsoft.EntityFrameworkCore;
using StudentsSqlite.Models;

namespace StudentsSqlite.Data
{
    /// <summary>
    /// Контекст данных Entity Framework Core.
    /// Класс связывает объектную модель приложения с базой данных SQLite:
    /// свойство Students соответствует таблице Students, а объект класса Student —
    /// строке этой таблицы.
    /// </summary>
    public sealed class AppDbContext : DbContext
    {
        private readonly string _databasePath;

        public AppDbContext(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Не задан путь к файлу базы данных", "databasePath");
            }

            _databasePath = databasePath;
        }

        /// <summary>Набор сущностей, отображаемый на таблицу Students.</summary>
        public DbSet<Student> Students { get; set; }

        /// <summary>
        /// Настройка подключения. Провайдер SQLite подключается методом UseSqlite;
        /// строка подключения содержит только путь к файлу базы данных.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=" + _databasePath);
            }
        }

        /// <summary>
        /// Описание схемы таблицы средствами Fluent API: первичный ключ,
        /// обязательные поля, максимальная длина строковых столбцов.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");

                entity.HasKey(student => student.Id);

                entity.Property(student => student.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(student => student.FirstName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(student => student.LastName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(student => student.MiddleName)
                      .HasMaxLength(50);

                entity.Property(student => student.Age)
                      .IsRequired();

                // Вычисляемое свойство в базе данных не хранится
                entity.Ignore(student => student.FullName);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
