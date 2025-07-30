using SAFERR.DTOs;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface IVerificationLogRepository : IGenericRepository<VerificationLog>
{
    // ... existing methods ...
    Task<IEnumerable<VerificationLog>> GetBySecurityCodeIdAsync(Guid securityCodeId);
    Task<IEnumerable<VerificationLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetTotalVerificationsAsync();
    Task<Dictionary<VerificationResult, int>> GetVerificationResultCountsAsync();
        
    // --- Updated/Added methods for brand-specific reporting ---
    // Get logs within a date range for a specific brand
    Task<IEnumerable<VerificationLog>> GetByDateRangeForBrandAsync(Guid brandId, DateTime startDate, DateTime endDate);
        
    // Get total verifications for a specific brand
    Task<int> GetTotalVerificationsForBrandAsync(Guid brandId);
        
    // Get verification result counts for a specific brand
    Task<Dictionary<VerificationResult, int>> GetVerificationResultCountsForBrandAsync(Guid brandId);
        
    // Get suspicious activities (codes verified > 1 time) for a specific brand
    Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesForBrandAsync(Guid brandId, int limit = 10);
        
    // Get top verified products for a specific brand
    Task<IEnumerable<ProductVerificationCount>> GetTopVerifiedProductsForBrandAsync(Guid brandId, int limit = 10);
    // ---------------------------------------------------------------------
}