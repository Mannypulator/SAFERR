using System;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
{
    Task<SubscriptionPlan?> GetByNameAsync(string name);
    Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync();
}
