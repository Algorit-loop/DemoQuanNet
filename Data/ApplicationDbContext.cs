using Microsoft.EntityFrameworkCore;
using QuanNet.Models;

namespace QuanNet.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Computer> Computers { get; set; }
    public DbSet<ComputerUsage> ComputerUsages { get; set; }

    public async Task SeedInitialDataAsync()
    {
        if (!Users.Any())
        {
            // Create admin user
            var adminUser = new User
            {
                Username = "admin",
                Name = "Admin", 
                PhoneNumber = "admin",
                Password = "admin",
                Balance = 0,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };
            Users.Add(adminUser);
            
            // Create some sample computers
            var computers = new[]
            {
                new Computer { Name = "Máy 01", Status = ComputerStatus.Available, HourlyRate = 15000 },
                new Computer { Name = "Máy 02", Status = ComputerStatus.Available, HourlyRate = 15000 },
                new Computer { Name = "Máy 03", Status = ComputerStatus.Available, HourlyRate = 15000 },
                new Computer { Name = "Máy 04", Status = ComputerStatus.Available, HourlyRate = 15000 },
                new Computer { Name = "Máy 05", Status = ComputerStatus.Available, HourlyRate = 15000 }
            };
            Computers.AddRange(computers);

            await SaveChangesAsync();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Balance)
            .HasPrecision(18, 2);

        // Configure Computer
        modelBuilder.Entity<Computer>()
            .Property(c => c.HourlyRate)
            .HasPrecision(18, 2);

        // Configure ComputerUsage
        modelBuilder.Entity<ComputerUsage>()
            .Property(cu => cu.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ComputerUsage>()
            .HasOne(cu => cu.User)
            .WithMany(u => u.ComputerUsages)
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComputerUsage>()
            .HasOne(cu => cu.Computer)
            .WithMany(c => c.ComputerUsages)
            .HasForeignKey(cu => cu.ComputerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}