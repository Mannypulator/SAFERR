using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    // Add Product-specific methods if needed
    Task<IEnumerable<Product>> GetByBrandIdAsync(Guid brandId);
}