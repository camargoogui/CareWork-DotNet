using Microsoft.EntityFrameworkCore;
using CareWork.Infrastructure.Data.Configurations;
using CareWork.Infrastructure.Models;

namespace CareWork.Infrastructure.Data;

public class CareWorkDbContext : DbContext
{
    public CareWorkDbContext(DbContextOptions<CareWorkDbContext> options) : base(options)
    {
    }

    public DbSet<Checkin> Checkins { get; set; }
    public DbSet<Tip> Tips { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CheckinConfiguration());
        modelBuilder.ApplyConfiguration(new TipConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}

