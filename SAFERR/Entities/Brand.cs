using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAFERR.Entities;

public class Brand
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid(); // Using GUID for global uniqueness

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ContactEmail { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    // --- Add Subscription Link ---
    public Guid? CurrentSubscriptionId { get; set; }

    [ForeignKey(nameof(CurrentSubscriptionId))]
    public BrandSubscription? CurrentSubscription { get; set; }
    // ----------------------------

    // Navigation Properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<BrandSubscription> Subscriptions { get; set; } = new List<BrandSubscription>();
}
