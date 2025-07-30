using Microsoft.EntityFrameworkCore;
using SAFERR.Entities;

namespace SAFERR.Data;


public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<SecurityCode> SecurityCodes { get; set; }
    public DbSet<VerificationLog> VerificationLogs { get; set; }
    // --- Add new DbSets ---
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<BrandSubscription> BrandSubscriptions { get; set; }

    public DbSet<User> Users => Set<User>();
    // ---------------------

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configure SecurityCode ---
        modelBuilder.Entity<SecurityCode>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<SecurityCode>()
            .HasIndex(s => s.ProductId);

        modelBuilder.Entity<SecurityCode>()
            .HasIndex(s => s.IsVerified);
        // -----------------------------

        // --- Configure VerificationLog ---
        modelBuilder.Entity<VerificationLog>()
            .HasIndex(v => v.CodeAttempted);

        modelBuilder.Entity<VerificationLog>()
            .HasIndex(v => new { v.VerificationAttemptedAt, v.Result });

        modelBuilder.Entity<VerificationLog>()
            .HasIndex(v => v.VerificationAttemptedAt);

        modelBuilder.Entity<VerificationLog>()
            .HasIndex(v => v.SecurityCodeId);

        modelBuilder.Entity<VerificationLog>()
      .HasIndex(v => v.SourcePhoneNumber);
        // ---------------------------------
        // ---------------------------------

        // --- Configure Brand ---
        modelBuilder.Entity<Brand>()
            .HasIndex(b => b.Name)
            .IsUnique();

        // Define relationship: Brand 1 -> 0..1 CurrentSubscription
        modelBuilder.Entity<Brand>()
            .HasOne(b => b.CurrentSubscription)
            .WithOne() // One-to-zero-or-one
            .HasForeignKey<Brand>(b => b.CurrentSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull); // If subscription is deleted, set FK to null
                                               // -----------------------

        // --- Configure Product ---
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.BrandId);
        // -------------------------

        // --- Configure relationships explicitly ---
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SecurityCode>()
            .HasOne(sc => sc.Product)
            .WithMany(p => p.SecurityCodes)
            .HasForeignKey(sc => sc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VerificationLog>()
            .HasOne(vl => vl.SecurityCode)
            .WithMany(sc => sc.VerificationLogs)
            .HasForeignKey(vl => vl.SecurityCodeId)
            .OnDelete(DeleteBehavior.SetNull);
        // ---------------------------------------

        // --- Configure SubscriptionPlan ---
        modelBuilder.Entity<SubscriptionPlan>()
            .HasIndex(sp => sp.Name)
            .IsUnique();
        // ---------------------------------

        // --- Configure BrandSubscription ---
        modelBuilder.Entity<BrandSubscription>()
       .HasIndex(bs => new { bs.BrandId, bs.Status, bs.StartDate, bs.EndDate });

        // Index for updating usage (by Id)
        modelBuilder.Entity<BrandSubscription>()
            .HasIndex(bs => bs.Id);

        // Define relationship: Brand 1 -> Many BrandSubscriptions
        modelBuilder.Entity<BrandSubscription>()
            .HasOne(bs => bs.Brand)
            .WithMany(b => b.Subscriptions) // Brand.Subscriptions
            .HasForeignKey(bs => bs.BrandId)
            .OnDelete(DeleteBehavior.Cascade); // If brand is deleted, delete its subscriptions

        // Define relationship: SubscriptionPlan 1 -> Many BrandSubscriptions
        modelBuilder.Entity<BrandSubscription>()
            .HasOne(bs => bs.SubscriptionPlan)
            .WithMany(sp => sp.BrandSubscriptions) // SubscriptionPlan.BrandSubscriptions
            .HasForeignKey(bs => bs.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a plan if subscriptions exist
                                                // ------------------------------------

        modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.BrandId);

        // Relationship: User N -> 1 Brand
        modelBuilder.Entity<User>()
            .HasOne(u => u.Brand)
            .WithMany() // Or b => b.Users if you add ICollection<User> Users to Brand
            .HasForeignKey(u => u.BrandId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

