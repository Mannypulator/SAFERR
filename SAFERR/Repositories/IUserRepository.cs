using System;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UserExistsAsync(string username, string email); // Check uniqueness
                                                               // Add other user-specific methods as needed
}
