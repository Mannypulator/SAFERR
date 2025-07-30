using SAFERR.DTOs;
using SAFERR.Entities;

namespace SAFERR.Services;

public interface ISecurityCodeService
{
    // Generates a single unique code
    Task<string> GenerateUniqueCodeAsync();

    // Generates multiple unique codes for a product
    Task<List<string>> GenerateCodesForProductAsync(Guid productId, int count);

    // Verifies a code based on user input (e.g., SMS content)
    Task<VerificationResultDto> VerifyCodeAsync(string codeInput, string? sourcePhoneNumber = null, string? sourceIpAddress = null);

    // Gets statistics (basic example)
    Task<SecurityCodeStatsDto> GetStatisticsAsync();

    Task<(bool IsAllowed, string Message)> CheckCodeGenerationPermissionAsync(Guid brandId, int requestedCount);
    Task<(bool IsAllowed, string Message)> CheckVerificationPermissionAsync(Guid brandId);

    Task<bool> SendSmsAsync(string toPhoneNumber, string message);


    // Task<VerificationTrendDto> GetVerificationTrendAsync(DateTime startDate, DateTime endDate);
    // Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesAsync(int limit = 10);
    // Task<ProductDistributionDto> GetProductDistributionAsync();
    
    Task<VerificationTrendDto> GetVerificationTrendForBrandAsync(Guid brandId, DateTime startDate, DateTime endDate);
        
    // Brand-specific suspicious activities
    Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesForBrandAsync(Guid brandId, int limit = 10);
        
    // Brand-specific product distribution
    Task<ProductDistributionDto> GetProductDistributionForBrandAsync(Guid brandId);
}

// DTOs for returning data from services
