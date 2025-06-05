using DormitoryPATDesktop.Context.Database;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryPATDesktop.Context
{
    public class DutyScheduleContext : DbContext
    {
        public DbSet<DutySchedule> DutySchedule { get; set; }
        public DutyScheduleContext()
        {
            Database.EnsureCreated();
            DutySchedule.Load();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.connection, Config.version);
        }
    }
}
