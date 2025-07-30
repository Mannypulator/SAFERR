using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class BrandRepository : GenericRepository<Brand>, IBrandRepository
{
    public BrandRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Brand?> GetByNameAsync(string name)
    {
        return await _context.Brands
            .FirstOrDefaultAsync(b => b.Name.ToLower() == name.ToLower());
    }
}