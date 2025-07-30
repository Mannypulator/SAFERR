using System;
using Microsoft.EntityFrameworkCore;
using SAFERR.Data;
using SAFERR.Entities;

namespace SAFERR.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
                             .Include(u => u.Brand) // Eager load brand for token generation
                             .FirstOrDefaultAsync(u => u.Username.Trim().ToLower() == username.Trim().ToLower());
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
                             .Include(u => u.Brand)
                             .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> UserExistsAsync(string username, string email)
    {
        return await _context.Users
                             .AnyAsync(u => u.Username == username || u.Email == email);
    }
}

