using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using SmartClosetAPI.Models;

namespace SmartClosetAPI.Models
{
    public class TodoContext : DbContext
    {
        public TodoContext(DbContextOptions<TodoContext> opcao)
        : base(opcao)
        {
        }

        public DbSet<Contas> Contas { get; set; } = null!;
		public DbSet<Roupas> Roupas { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<Roupas>()
				.HasOne(ot => ot.Contas)
				.WithMany(r => r.Roupas)
				.HasForeignKey(ot => ot.Id_conta)
				.HasConstraintName("fk_idconta");
		}
	}
}
