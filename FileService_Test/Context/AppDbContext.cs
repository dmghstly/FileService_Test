using FileService_Test.Models;
using Microsoft.EntityFrameworkCore;
using File = FileService_Test.Models.File;

namespace FileService_Test.Context
{
    // This dbContext that defines a structure of a Data Base inside SqlServer
    // Inside services ef core is used to communicate with data base
    // It has Users - table to store all users data
    // Groups - File group, which groups files
    // Files - table to store file data
    // RefLinks - table to store created links (its secret and to what it refers)
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<FileGroup> Groups { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<RefLink> RefLinks { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Setting unique filed Name for users
            modelBuilder.Entity<User>().HasIndex(u => u.Name).IsUnique();

            modelBuilder.Entity<User>().HasMany(q => q.Groups);
            modelBuilder.Entity<FileGroup>().HasMany(q => q.Files);
        }
    }
}
