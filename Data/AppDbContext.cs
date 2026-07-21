using Microsoft.EntityFrameworkCore;
using LegacyTaskManager.Api.Models;

namespace LegacyTaskManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "John Smith", Email = "john@example.com", Phone = "111-111-1111", userType = "Admin" },
                new User { Id = 2, Name = "Sarah Lee", Email = "sarah@example.com", Phone = "222-222-2222", userType = "Normal" },
                new User { Id = 3, Name = "Mike Brown", Email = "mike@example.com", Phone = "333-333-3333", userType = "Normal" }
            );

            modelBuilder.Entity<TaskItem>().HasData(
                new TaskItem { Id = 1, Title = "Setup laptop", Description = "Configure new laptop for onboarding", IsCompleted = false, Status = "Pending", AssignedUserId = 1, Priority = 1, CreatedBy = "system" },
                new TaskItem { Id = 2, Title = "Fix login bug", Description = "Users can't log in on mobile", IsCompleted = false, Status = "Pending", AssignedUserId = 2, Priority = 3, CreatedBy = "system" },
                new TaskItem { Id = 3, Title = "Write report", Description = "Monthly sales report", IsCompleted = true, Status = "Completed", AssignedUserId = 3, Priority = 2, CreatedBy = "system" },
                new TaskItem { Id = 4, Title = "Update docs", Description = "Refresh the API documentation", IsCompleted = false, Status = "Pending", AssignedUserId = null, Priority = 2, CreatedBy = "system" },
                new TaskItem { Id = 5, Title = "Server maintenance", Description = "Patch the production server", IsCompleted = false, Status = "Pending", AssignedUserId = 1, Priority = 1, CreatedBy = "system" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
