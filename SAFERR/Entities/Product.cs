using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAFERR.Entities;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)] // E.g., SKU, GTIN
    public string? Identifier { get; set; }

    // Foreign Key
    [Required]
    public Guid BrandId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(BrandId))]
    public Brand Brand { get; set; } = null!; // Required navigation property

    public ICollection<SecurityCode> SecurityCodes { get; set; } = new List<SecurityCode>();
}