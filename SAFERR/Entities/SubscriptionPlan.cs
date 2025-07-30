using System;
using System.ComponentModel.DataAnnotations;

namespace SAFERR.Entities;

public class SubscriptionPlan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // E.g., "Starter", "Professional", "Enterprise"

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
    public decimal Price { get; set; } // Annual price

    [Required]
    public int MaxCodesPerMonth { get; set; } // -1 or a very high number for "Unlimited"

    [Required]
    public int MaxVerificationsPerMonth { get; set; } // -1 or a very high number for "Unlimited"

    [Required]
    public bool IsActive { get; set; } = true; // Allows deactivating plans

    // Navigation Properties
    public ICollection<BrandSubscription> BrandSubscriptions { get; set; } = new List<BrandSubscription>();
}