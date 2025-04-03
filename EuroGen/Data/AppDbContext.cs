using EuroGen.Models;
using Microsoft.EntityFrameworkCore;

namespace EuroGen.Data;

public partial class AppDbContext : DbContext
{
    public DbSet<Draw> Draws { get; set; }

    public AppDbContext()
    {
        SQLitePCL.Batteries_V2.Init();
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "draws.db3");

        optionsBuilder
            .UseSqlite($"Filename={dbPath}");
    }
}
