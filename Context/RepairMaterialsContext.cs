using DormitoryPATDesktop.Context.Database;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;

namespace DormitoryPATDesktop.Context
{
    public class RepairMaterialsContext : DbContext
    {
        public DbSet<RepairMaterials> RepairMaterials { get; set; }

        public RepairMaterialsContext()
        {
            Database.EnsureCreated();
            RepairMaterials.Load();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.connection, Config.version);
        }
    }
}