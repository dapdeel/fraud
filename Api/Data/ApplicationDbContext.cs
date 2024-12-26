namespace Api.Data
{
    using Api.DTOs;
    using Api.Entity;
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
        public DbSet<TransactionRules> TransactionRules { get; set; }
        public DbSet<UserObservatory> UserObservatories { get; set; }
        public DbSet<TransactionCustomer> TransactionCustomers { get; set; }
        public DbSet<TransactionAccount> TransactionAccounts { get; set; }
        public DbSet<TransactionFileDocument> TransactionFileDocument {get;set;} 
        public DbSet<TransactionProfile> TransactionProfiles { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BlacklistedAccount> BlacklistedAccounts { get; set; }
        public DbSet<SuspiciousAccount> SuspiciousAccounts { get; set; }
        public DbSet<BlacklistedTransaction> BlacklistedTransactions { get; set; }
        public DbSet<SuspiciousTransaction> SuspiciousTransactions { get; set; }
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

            modelBuilder.Entity<TransactionCustomer>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<TransactionCustomer>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

           
            modelBuilder.Entity<TransactionAccount>()
            .HasOne<TransactionCustomer>(a => a.TransactionCustomer)
            .WithMany(c => c.TransactionAccounts).HasForeignKey(a => a.CustomerId);

            modelBuilder.Entity<TransactionAccount>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<TransactionAccount>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<TransactionProfile>()
            .HasOne<TransactionCustomer>(pr => pr.TransactionCustomer)
            .WithMany(c => c.TransactionProfiles).HasForeignKey(pr => pr.CustomerId);
           
            modelBuilder.Entity<TransactionProfile>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<TransactionProfile>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Transaction>()
            .HasOne<TransactionAccount>(t => t.DebitAccount)
            .WithMany().HasForeignKey(t => t.DebitAccountId);

            modelBuilder.Entity<Transaction>()
            .HasOne<TransactionAccount>(t => t.CreditAccount)
            .WithMany().HasForeignKey(t => t.CreditAccountId);
            modelBuilder.Entity<Transaction>()
            .HasOne<Observatory>(t => t.Observatory)
            .WithMany().HasForeignKey(t => t.ObservatoryId);

            modelBuilder.Entity<Transaction>().Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<Transaction>().Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
