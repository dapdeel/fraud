namespace Api.Data
{
    using Api.Models;
    using Gremlin.Net.Process.Traversal;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<Observatory> Observatories { get; set; }
        public DbSet<UserObservatory> UserObservatories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _ = modelBuilder.Entity<Observatory>()
            .HasMany(o => o.Users)
            .WithMany(u => u.Observatories)
            .UsingEntity<UserObservatory>(
                l => l.HasOne<ApplicationUser>(uo => uo.User).WithMany(u => u.UserObservatories).HasForeignKey(uo => uo.UserId),
                r => r.HasOne<Observatory>(uo => uo.Observatory).WithMany(o => o.UserObservatories).HasForeignKey(uo => uo.ObservatoryId)
            );
            modelBuilder.Entity<Observatory>().HasOne<Bank>(o => o.Bank).WithMany(b => b.Observatories).HasForeignKey(o => o.BankId);

            modelBuilder.Entity<Observatory>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<Observatory>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<UserObservatory>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<UserObservatory>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
