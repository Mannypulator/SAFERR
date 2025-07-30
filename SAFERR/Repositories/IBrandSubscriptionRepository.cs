using System;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface IBrandSubscriptionRepository : IGenericRepository<BrandSubscription>
{
    Task<BrandSubscription?> GetCurrentSubscriptionForBrandAsync(Guid brandId);
    Task<IEnumerable<BrandSubscription>> GetSubscriptionHistoryForBrandAsync(Guid brandId);
    Task UpdateUsageAsync(Guid brandSubscriptionId, int codesGeneratedDelta = 0, int verificationsReceivedDelta = 0);
}
