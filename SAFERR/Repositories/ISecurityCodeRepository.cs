using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface ISecurityCodeRepository : IGenericRepository<SecurityCode>
{
    // Find a code by its string value (critical for verification)
    Task<SecurityCode?> GetByCodeValueAsync(string codeValue);

    // Get codes for a specific product
    Task<IEnumerable<SecurityCode>> GetByProductIdAsync(Guid productId);

    // Check if a code already exists (important during generation)
    Task<bool> CodeExistsAsync(string codeValue);

    // Bulk generate codes (for efficiency)
    Task AddRangeAsync(IEnumerable<SecurityCode> codes);

    // Get statistics (e.g., total codes, verified codes) - useful for dashboards
    Task<int> GetTotalCountAsync();
    Task<int> GetVerifiedCountAsync();

    Task<HashSet<string>> FindExistingCodesAsync(IEnumerable<string> candidateCodes);
    // Add more stats methods as needed
}