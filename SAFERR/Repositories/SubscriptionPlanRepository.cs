using System;
using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string name)
    {
        return await _context.SubscriptionPlans
                             .FirstOrDefaultAsync(sp => sp.Name.ToLower() == name.ToLower() && sp.IsActive);
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync()
    {
        return await _context.SubscriptionPlans
                             .Where(sp => sp.IsActive)
                             .ToListAsync();
    }
}
