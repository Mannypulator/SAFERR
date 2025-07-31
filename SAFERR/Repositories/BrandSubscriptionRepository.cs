using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class BrandSubscriptionRepository : GenericRepository<BrandSubscription>, IBrandSubscriptionRepository
{
    private readonly IMemoryCache _cache; // Add this field
    private readonly ILogger<BrandSubscriptionRepository> _logger;
    public BrandSubscriptionRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<BrandSubscriptionRepository> logger) : base(context)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<BrandSubscription?> GetCurrentSubscriptionForBrandAsync(Guid brandId)
    {
        // Assumes Brand.CurrentSubscription is eager loaded or that we fetch it directly
        // This query finds the active subscription based on date and status

        // --- Implement Caching ---
        var cacheKey = $"CurrentSubscription_{brandId}";

        if (_cache.TryGetValue(cacheKey, out BrandSubscription? cachedSubscription))
        {
            _logger.LogDebug("Current subscription for Brand ID {BrandId} retrieved from cache.", brandId);
            return cachedSubscription;
        }

        // If not in cache, fetch from database
        var now = DateTime.UtcNow;
        var subscription = await _context.BrandSubscriptions
                .Include(bs => bs.SubscriptionPlan)
                .FirstOrDefaultAsync(bs =>
                    bs.BrandId == brandId &&
                    bs.Status == SubscriptionStatus.Active &&
                    bs.StartDate <= now &&
                    (bs.EndDate == null || bs.EndDate > now)
            );

        if (subscription != null)
        {
            // Cache the result for 5 minutes (adjust TTL as needed)
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, subscription, cacheEntryOptions);
            _logger.LogDebug("Current subscription for Brand ID {BrandId} fetched from database and cached.", brandId);
        }
        else
        {
            _logger.LogDebug("No current subscription found for Brand ID {BrandId}.", brandId);
        }

        return subscription;
    }

    public async Task<IEnumerable<BrandSubscription>> GetSubscriptionHistoryForBrandAsync(Guid brandId)
    {
        return await _context.BrandSubscriptions
                             .Where(bs => bs.BrandId == brandId)
                             .OrderByDescending(bs => bs.StartDate)
                             .ToListAsync();
    }

    public async Task UpdateUsageAsync(Guid brandSubscriptionId, int codesGeneratedDelta = 0, int verificationsReceivedDelta = 0)
    {
        // --- Invalidate Cache on Update ---
        // When usage is updated, invalidate the cache for the associated brand
        // We need to find the BrandId first. This requires a DB lookup.
        // To avoid this lookup on every update, consider storing BrandId in cache key
        // or using a more sophisticated caching strategy (e.g., cache tag invalidation if supported).
        // For simplicity here, we'll do the lookup when updating usage.

        // Find the subscription to get the BrandId
        var subscription = await _context.BrandSubscriptions.AsNoTracking().FirstOrDefaultAsync(bs => bs.Id == brandSubscriptionId);
        if (subscription != null)
        {
            var cacheKey = $"CurrentSubscription_{subscription.BrandId}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Cache invalidated for Brand ID {BrandId} due to usage update on Subscription ID {SubscriptionId}.", subscription.BrandId, brandSubscriptionId);
            
        }
        // -----------------------------------
        
        // subscription.CodesGenerated = codesGeneratedDelta;
        // subscription.VerificationsReceived = verificationsReceivedDelta;
        // _context.BrandSubscriptions.Update(subscription);
        // await _context.SaveChangesAsync();
     

        // await _context.Database.ExecuteSqlRawAsync(@"
        //         UPDATE BrandSubscriptions
        //         SET CodesGenerated = CodesGenerated + {0},
        //             VerificationsReceived = VerificationsReceived + {1}
        //         WHERE Id = {2}",
        //     codesGeneratedDelta, verificationsReceivedDelta, brandSubscriptionId);
    }

}
