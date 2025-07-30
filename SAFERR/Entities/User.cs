using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SAFERR.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    // Store hashed password
    [JsonIgnore] // Don't serialize password
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public Guid BrandId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(BrandId))]
    public Brand Brand { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
