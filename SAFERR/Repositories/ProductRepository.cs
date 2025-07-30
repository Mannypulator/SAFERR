using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetByBrandIdAsync(Guid brandId)
    {
        return await _context.Products
            .Where(p => p.BrandId == brandId)
            .ToListAsync();
    }
}