using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class SecurityCodeRepository : GenericRepository<SecurityCode>, ISecurityCodeRepository
{
    public SecurityCodeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SecurityCode?> GetByCodeValueAsync(string codeValue)
    {
        return await _context.SecurityCodes
            .Include(sc => sc.Product) // Eager load Product if needed in verification
            .FirstOrDefaultAsync(sc => sc.Code == codeValue);
    }

    public async Task<IEnumerable<SecurityCode>> GetByProductIdAsync(Guid productId)
    {
        return await _context.SecurityCodes
            .Where(sc => sc.ProductId == productId)
            .ToListAsync();
    }

    public async Task<bool> CodeExistsAsync(string codeValue)
    {
        return await _context.SecurityCodes.AnyAsync(sc => sc.Code == codeValue);
    }

    public async Task AddRangeAsync(IEnumerable<SecurityCode> codes)
    {
        await _context.SecurityCodes.AddRangeAsync(codes);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.SecurityCodes.CountAsync();
    }

    public async Task<int> GetVerifiedCountAsync()
    {
        return await _context.SecurityCodes.CountAsync(sc => sc.IsVerified);
    }

    public async Task<HashSet<string>> FindExistingCodesAsync(IEnumerable<string> candidateCodes)
    {
        // Use EF Core's Contains for efficient batch lookup
        var existingCodes = _context.SecurityCodes
            .Where(sc => candidateCodes.Contains(sc.Code))
            .Select(sc => sc.Code)
            .ToHashSet(); // Fetch as HashSet for fast lookup

        return await Task.FromResult(existingCodes);
    }
}
