using System;
using System.ComponentModel.DataAnnotations;
using SAFERR.Entities;

namespace SAFERR.DTOs;

public class VerificationResultDto
{
    public VerificationResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? SecurityCodeId { get; set; } // ID of the verified code, if genuine
    public Guid? ProductId { get; set; } // ID of the associated product, if genuine
    public string? ProductName { get; set; } // Name of the product, if genuine
    public Guid? BrandId { get; set; } // ID of the associated brand, if genuine
    public string? BrandName { get; set; } // Name of the brand, if genuine
}

public class SecurityCodeStatsDto
{
    public int TotalCodesGenerated { get; set; }
    public int TotalCodesVerified { get; set; }
    public int TotalVerificationsAttempted { get; set; }
    public Dictionary<VerificationResult, int> VerificationResults { get; set; } = new();
}

public class VerificationTrendDto
{
    public List<DailyVerificationCount> DailyCounts { get; set; } = new();
    public int TotalVerifications { get; set; }
}

public class DailyVerificationCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class SuspiciousActivityDto
{
    public Guid SecurityCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int VerificationCount { get; set; }
    public DateTime FirstVerification { get; set; }
    public DateTime LastVerification { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    // Add SourcePhoneNumber if tracking multiple sources is needed
}

public class ProductDistributionDto
{
    public List<ProductVerificationCount> TopProducts { get; set; } = new();
    public int TotalProducts { get; set; }
}

public class ProductVerificationCount
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int VerificationCount { get; set; }
}

public class VerifyCodeRequest
{
    [Required]
    public string? Code { get; set; }

    public string? SourcePhoneNumber { get; set; }
}

public class LoginModel
{
    [Required]
    public string Username { get; set; } = string.Empty; // Or Email

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string BrandName { get; set; } = string.Empty; // Associate user with a new/existing brand
}

public class AuthResponseModel
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid BrandId { get; set; } // The brand the user belongs to
    public string BrandName { get; set; } = string.Empty;
}

