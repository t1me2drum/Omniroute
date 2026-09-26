using Microsoft.EntityFrameworkCore;
using Omniroute.Models;

namespace Omniroute.Data;

/// <summary>
/// Контекст бази даних для історії
/// </summary>
public class HistoryDbContext : DbContext
{
    private readonly string _dbPath;

    public DbSet<HistoryEntry> History { get; set; } = null!;

    public HistoryDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "Omniroute");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "history.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SerialNumber, e.Timestamp });
            entity.Property(e => e.Timestamp).HasDefaultValueSql("datetime('now')");
        });
    }

    /// <summary>
    /// Додати запис телеметрії
    /// </summary>
    public async Task AddEntryAsync(HistoryEntry entry)
    {
        History.Add(entry);
        await SaveChangesAsync();
    }

    /// <summary>
    /// Отримати історію для пристрою за період
    /// </summary>
    public async Task<List<HistoryEntry>> GetHistoryAsync(string serialNumber, DateTime from, DateTime to)
    {
        return await History
            .Where(h => h.SerialNumber == serialNumber && h.Timestamp >= from && h.Timestamp <= to)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Очистити стару історію (старше 30 днів)
    /// </summary>
    public async Task CleanupOldEntriesAsync()
    {
        var threshold = DateTime.Now.AddDays(-30);
        var oldEntries = History.Where(h => h.Timestamp < threshold);
        History.RemoveRange(oldEntries);
        await SaveChangesAsync();
    }
}
