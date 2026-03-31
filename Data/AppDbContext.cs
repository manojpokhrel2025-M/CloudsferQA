using CloudsferQA.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudsferQA.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TestSession> Sessions     => Set<TestSession>();
    public DbSet<TestResult>  Results      => Set<TestResult>();
    public DbSet<TestCase>    TestCases    => Set<TestCase>();
    public DbSet<User>        Users        => Set<User>();
    public DbSet<ModuleOrder> ModuleOrders => Set<ModuleOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TestResult → TestSession (cascade delete)
        modelBuilder.Entity<TestResult>()
            .HasOne(r => r.Session)
            .WithMany(s => s.Results)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one result per test case per session
        modelBuilder.Entity<TestResult>()
            .HasIndex(r => new { r.SessionId, r.TestCaseId })
            .IsUnique();

        // TestSession → User (set null on delete so sessions survive user deletion)
        modelBuilder.Entity<TestSession>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // User email must be unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // ModuleOrder primary key
        modelBuilder.Entity<ModuleOrder>()
            .HasKey(m => m.ModuleName);
    }
}
