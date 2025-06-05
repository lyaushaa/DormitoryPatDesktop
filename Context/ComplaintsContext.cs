using DormitoryPATDesktop.Context.Database;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;

namespace DormitoryPATDesktop.Context
{
    public class ComplaintsContext : DbContext
    {
        public DbSet<Complaints> Complaints { get; set; }
        public ComplaintsContext()
        {
            Database.EnsureCreated();
            Complaints.Load();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.connection, Config.version);
        }
    }
}
