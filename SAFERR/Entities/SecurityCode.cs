using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAFERR.Entities;

public class SecurityCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid(); // Internal primary key

    [Required]
    [MaxLength(50)] // Adjust size based on code generation strategy (e.g., 12 chars)
    [Column(TypeName = "varchar(50)")] // Ensure efficient storage for codes
    public string Code { get; set; } = string.Empty; // The unique alphanumeric code

    [Required]
    public Guid ProductId { get; set; } // Link to the specific product unit

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // When the code was generated

    // Optional: Track if the code has been printed/used for physical application
    public bool IsApplied { get; set; } = false;

    // Optional: Track if the code has been verified at least once
    public bool IsVerified { get; set; } = false;

    // Optional: Track the first verification timestamp
    public DateTime? FirstVerifiedAt { get; set; }

    // Optional: Track if the code was flagged during verification (e.g., multiple checks)
    public bool IsFlagged { get; set; } = false;
    

    // Navigation Properties
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!; // Required navigation property

    public ICollection<VerificationLog> VerificationLogs { get; set; } = new List<VerificationLog>();
}
