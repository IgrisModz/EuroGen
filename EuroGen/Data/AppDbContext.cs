using EuroGen.Models;
using Microsoft.EntityFrameworkCore;

namespace EuroGen.Data;

public partial class AppDbContext : DbContext
{
	public DbSet<Draw> Draws { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
		SQLitePCL.Batteries_V2.Init();
		Database.EnsureCreated();
	}
}