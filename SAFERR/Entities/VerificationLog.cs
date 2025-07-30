using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAFERR.Entities;

public class VerificationLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)] // Should match SecurityCode.Code length
    [Column(TypeName = "varchar(50)")]
    public string CodeAttempted { get; set; } = string.Empty; // The code sent by the user

    [Required]
    public DateTime VerificationAttemptedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public VerificationResult Result { get; set; }

    // Optional: Store metadata like IP address, SMS sender number (be mindful of privacy)
    [MaxLength(45)] // IPv4 or IPv6
    public string? SourceIpAddress { get; set; }

    [MaxLength(20)] // E.164 format
    public string? SourcePhoneNumber { get; set; }

    // Optional: Link to the actual SecurityCode if found (allows for easier querying of genuine attempts)
    public Guid? SecurityCodeId { get; set; } // Nullable, as invalid codes won't link

    // Navigation Properties
    [ForeignKey(nameof(SecurityCodeId))]
    public SecurityCode? SecurityCode { get; set; } // Optional navigation property
}

public enum VerificationResult
{
    Genuine,
    Counterfeit,
    AlreadyVerified, // Optional: if you want to distinguish first-time vs repeat
    InvalidCodeFormat, // Optional: if format is checked before DB lookup
    Error
}