using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface IBrandRepository : IGenericRepository<Brand>
{
    // Add Brand-specific methods if needed
    Task<Brand?> GetByNameAsync(string name);
}