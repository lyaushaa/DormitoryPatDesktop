﻿using DormitoryPATDesktop.Context.Database;
using DormitoryPATDesktop.Models;
using Microsoft.EntityFrameworkCore;

namespace DormitoryPATDesktop.Context
{
    public class DutyEducatorsContext : DbContext
    {
        public DbSet<DutyEducators> DutyEducators { get; set; }
        public DutyEducatorsContext()
        {
            Database.EnsureCreated();
            DutyEducators.Load();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.connection, Config.version);
        }
    }
}
