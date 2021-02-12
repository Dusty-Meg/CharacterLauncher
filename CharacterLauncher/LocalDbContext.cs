using System;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace CharacterLauncher
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext()
        {
            
        }

        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
            
        }

        public virtual DbSet<CharacterInfo> Users { get; set; }
        public virtual DbSet<CharacterOnline> Online { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("host=127.0.0.1;port=3306;user id=seat;password=srgHN9uefEXXbucH;database=seat;SslMode=None",
                new MariaDbServerVersion(
                    new System.Version(10,3,22)
                ));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterInfo>(entity =>
            {
                entity.HasKey(key => key.character_id);
                entity.ToTable("character_infos");

            });

            modelBuilder.Entity<CharacterOnline>(entity =>
            {
                entity.HasKey(key => key.character_id);
                entity.ToTable("character_onlines");

            });
        }
    }
}
