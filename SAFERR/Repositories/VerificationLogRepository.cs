using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.DTOs;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class VerificationLogRepository : GenericRepository<VerificationLog>, IVerificationLogRepository
{
    public VerificationLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<VerificationLog>> GetByDateRangeForBrandAsync(Guid brandId, DateTime startDate, DateTime endDate)
    {
        // This requires joining VerificationLog -> SecurityCode -> Product -> Brand
        return await _context.VerificationLogs
            .Include(vl => vl.SecurityCode)
            .Where(vl => vl.SecurityCode != null &&
                         vl.SecurityCode.Product.BrandId == brandId &&
                         vl.VerificationAttemptedAt >= startDate &&
                         vl.VerificationAttemptedAt <= endDate)
            .ToListAsync();
    }
    
    public async Task<int> GetTotalVerificationsForBrandAsync(Guid brandId)
    {
        return await _context.VerificationLogs
            .Include(vl => vl.SecurityCode)
            .CountAsync(vl => vl.SecurityCode != null && vl.SecurityCode.Product.BrandId == brandId);
    }
    
    public async Task<Dictionary<VerificationResult, int>> GetVerificationResultCountsForBrandAsync(Guid brandId)
    {
        var results = await _context.VerificationLogs
            .Include(vl => vl.SecurityCode)
            .Where(vl => vl.SecurityCode != null && vl.SecurityCode.Product.BrandId == brandId)
            .GroupBy(vl => vl.Result)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToListAsync();

        return results.ToDictionary(r => r.Result, r => r.Count);
    }


    public async Task<IEnumerable<VerificationLog>> GetBySecurityCodeIdAsync(Guid securityCodeId)
    {
        return await _context.VerificationLogs
            .Where(vl => vl.SecurityCodeId == securityCodeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<VerificationLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.VerificationLogs
            .Where(vl => vl.VerificationAttemptedAt >= startDate && vl.VerificationAttemptedAt <= endDate)
            .ToListAsync();
    }

    public async Task<int> GetTotalVerificationsAsync()
    {
        return await _context.VerificationLogs.CountAsync();
    }

    public async Task<Dictionary<VerificationResult, int>> GetVerificationResultCountsAsync()
    {
        // Group by Result and count occurrences
        var results = await _context.VerificationLogs
            .GroupBy(vl => vl.Result)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToListAsync();

        // Convert to Dictionary
        return results.ToDictionary(r => r.Result, r => r.Count);
    }

    public async Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesForBrandAsync(Guid brandId, int limit = 10)
        {
            // Find SecurityCodeIds for the brand that have more than one VerificationLog entry
            var suspiciousActivities = await _context.VerificationLogs
                .Include(vl => vl.SecurityCode)
                .Where(vl => vl.SecurityCode != null && vl.SecurityCode.Product.BrandId == brandId && vl.SecurityCodeId.HasValue)
                .GroupBy(vl => vl.SecurityCodeId)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    CodeId = g.Key,
                    Count = g.Count(),
                    FirstVerification = g.Min(v => v.VerificationAttemptedAt),
                    LastVerification = g.Max(v => v.VerificationAttemptedAt)
                })
                .OrderByDescending(a => a.Count)
                .Take(limit)
                .Join(_context.SecurityCodes,
                      activity => activity.CodeId,
                      code => code.Id,
                      (activity, code) => new { Activity = activity, Code = code })
                .Join(_context.Products,
                      ac => ac.Code.ProductId,
                      product => product.Id,
                      (ac, product) => new { ac.Activity, ac.Code, Product = product })
                .Join(_context.Brands,
                      acp => acp.Product.BrandId,
                      brand => brand.Id,
                      (acp, brand) => new SuspiciousActivityDto
                      {
                          SecurityCodeId = acp.Activity.CodeId.Value,
                          Code = acp.Code.Code,
                          VerificationCount = acp.Activity.Count,
                          FirstVerification = acp.Activity.FirstVerification,
                          LastVerification = acp.Activity.LastVerification,
                          ProductId = acp.Product.Id,
                          ProductName = acp.Product.Name,
                          BrandId = acp.Product.BrandId, // This should match the input brandId
                          BrandName = brand.Name
                      })
                .ToListAsync();

            return suspiciousActivities;
        }


 

public async Task<IEnumerable<ProductVerificationCount>> GetTopVerifiedProductsForBrandAsync(Guid brandId, int limit = 10)
{
    var topProducts = await _context.VerificationLogs
         .Include(vl => vl.SecurityCode)
         // Filter by brand first to reduce the dataset early
         .Where(vl => vl.SecurityCode != null &&
                      vl.SecurityCode.Product.BrandId == brandId &&
                      vl.SecurityCodeId.HasValue)
         .Join(_context.SecurityCodes,
               vl => vl.SecurityCodeId,
               sc => sc.Id,
               (vl, sc) => new { VerificationLog = vl, SecurityCode = sc })
         .Join(_context.Products,
               vsc => vsc.SecurityCode.ProductId,
               p => p.Id,
               (vsc, p) => new { vsc.VerificationLog, vsc.SecurityCode, Product = p })
         // The Brand join might be redundant if you are already filtering by brandId,
         // but it's kept here as the original query included it. 
         // If Product.BrandId is sufficient, you could simplify.
         .Join(_context.Brands,
               vscp => vscp.Product.BrandId,
               b => b.Id,
               (vscp, b) => new { vscp.VerificationLog, vscp.SecurityCode, vscp.Product, Brand = b })
         // --- FIX: Explicitly name properties in the anonymous type ---
         .GroupBy(joined => new {
             ProductId = joined.Product.Id,     // Explicitly name ProductId
             ProductName = joined.Product.Name, // Explicitly name ProductName
             BrandId = joined.Brand.Id,         // Explicitly name BrandId
             BrandName = joined.Brand.Name      // Explicitly name BrandName
         })
         // ------------------------------------------------------------
         .Select(g => new ProductVerificationCount
         {
             // --- FIX: Access the explicitly named properties from the key ---
             ProductId = g.Key.ProductId,
             ProductName = g.Key.ProductName,
             BrandId = g.Key.BrandId,
             BrandName = g.Key.BrandName,
             // ----------------------------------------------------------------
             VerificationCount = g.Count()
         })
         .OrderByDescending(pvc => pvc.VerificationCount)
         .Take(limit)
         .ToListAsync();

    return topProducts;
}




}
