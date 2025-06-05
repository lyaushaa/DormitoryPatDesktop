using DormitoryPATDesktop.Context.Database;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;

namespace DormitoryPATDesktop.Context
{
    public class StudentsContext : DbContext
    {
        public DbSet<Students> Students { get; set; }
        public StudentsContext()
        {
            Database.EnsureCreated();
            Students.Load();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.connection, Config.version);
        }
    }
}
