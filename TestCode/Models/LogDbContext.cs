using Microsoft.EntityFrameworkCore;

namespace TestCode.Models
{
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
        {
        }
        public DbSet<UploadLog> uploadLog { get; set; }
        public DbSet<Addlog> addLog { get; set; }

        public DbSet<EditLog> editLog { get; set; }

        public DbSet<DeleteLog> deleteLog { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UploadLog>()
                .HasKey(u => u.log_id);
            modelBuilder.Entity<Addlog>()
                .HasKey(u => u.log_id);
            modelBuilder.Entity<EditLog>()
                .HasKey(u => u.log_id);
            modelBuilder.Entity<DeleteLog>()
                .HasKey(u => u.log_id);
        }
    }
}

