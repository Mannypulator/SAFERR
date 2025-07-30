using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAFERR.Entities;

public class BrandSubscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BrandId { get; set; }

    [Required]
    public Guid SubscriptionPlanId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; } // Null for ongoing subscriptions

    [Required]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [Required]
    [Column(TypeName = "decimal(18,2)")] // Standard for currency
    public decimal AmountPaid { get; set; }

    [MaxLength(100)] // E.g., "Invoice #12345", "Stripe Payment Intent ID"
    public string? PaymentReference { get; set; }

    // Track usage within the billing period
    public int CodesGenerated { get; set; } = 0;
    public int VerificationsReceived { get; set; } = 0; // Count successful verifications for the brand

    // Navigation Properties
    [ForeignKey(nameof(BrandId))]
    public Brand Brand { get; set; } = null!;

    [ForeignKey(nameof(SubscriptionPlanId))]
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}

public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled,
    Suspended // E.g., for non-payment
}

