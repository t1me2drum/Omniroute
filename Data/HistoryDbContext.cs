using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Контекст бази даних для історії.
/// DbContext не потокобезпечний, тому на кожну операцію створюється новий екземпляр.
/// </summary>
public class HistoryDbContext : DbContext
{
    private static readonly string DbPath = Path.Combine(LocalStore.AppFolder, "history.db");

    public DbSet<HistoryEntry> History { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SerialNumber, e.Timestamp });
        });
    }

    /// <summary>
    /// Створити файл і схему бази, якщо їх ще немає
    /// </summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var db = new HistoryDbContext();
        db.Database.EnsureCreated();
        AddMissingColumns(db);
    }

    /// <summary>
    /// EnsureCreated не змінює наявну базу, тож нові стовпці додаємо самі
    /// (усі вони допускають NULL, старі записи лишаються без цих значень)
    /// </summary>
    private static void AddMissingColumns(HistoryDbContext db)
    {
        var wanted = new Dictionary<string, string>
        {
            ["SolarWatts"] = "INTEGER",
            ["AcInWatts"] = "INTEGER",
            ["Grid"] = "INTEGER"
        };

        var connection = db.Database.GetDbConnection();
        connection.Open();
        try
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(History)";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    existing.Add(reader.GetString(1));
            }

            foreach (var (column, type) in wanted)
            {
                if (existing.Contains(column))
                    continue;
                using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE History ADD COLUMN {column} {type} NULL";
                alter.ExecuteNonQuery();
            }
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Додати запис телеметрії
    /// </summary>
    public static async Task AddEntryAsync(HistoryEntry entry)
    {
        await using var db = new HistoryDbContext();
        db.History.Add(entry);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Отримати історію для пристрою за період
    /// </summary>
    public static async Task<List<HistoryEntry>> GetHistoryAsync(string serialNumber, DateTime from, DateTime to)
    {
        await using var db = new HistoryDbContext();
        return await db.History
            .AsNoTracking()
            .Where(h => h.SerialNumber == serialNumber && h.Timestamp >= from && h.Timestamp <= to)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Видалити записи, старші за вказану кількість днів (без завантаження в пам'ять)
    /// </summary>
    public static async Task<int> CleanupOldEntriesAsync(int keepDays = 30)
    {
        var threshold = DateTime.Now.AddDays(-keepDays);
        await using var db = new HistoryDbContext();
        return await db.History.Where(h => h.Timestamp < threshold).ExecuteDeleteAsync();
    }
}
