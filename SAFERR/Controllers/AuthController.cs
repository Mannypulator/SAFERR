using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SAFERR.Data;
using SAFERR.DTOs;
using SAFERR.Entities;
using SAFERR.Repositories;
using SAFERR.Services;

namespace SAFERR.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: /api/auth
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository; // You need to create this
        private readonly IBrandRepository _brandRepository; // To create/get brand
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthController> _logger;
        // private readonly JwtSettings _jwtSettings; // Injected settings
        private readonly ApplicationDbContext _context;

        public AuthController(
            IUserRepository userRepository,
            IBrandRepository brandRepository,
            IPasswordHasher passwordHasher,
            ILogger<AuthController> logger,
            // IOptions<JwtSettings> jwtOptions, 
            ApplicationDbContext context) // Inject settings
        {
            _userRepository = userRepository;
            _brandRepository = brandRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _context = context;
            // _jwtSettings = jwtOptions.Value;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseModel>> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. Find user by username/email (adjust logic as needed)
                // For simplicity, let's assume login by username
                var user = await _userRepository.GetByUsernameAsync(model.Username);

                if (user == null)
                {
                    _logger.LogWarning("Login failed for username '{Username}'. User not found.", model.Username);
                    // Don't reveal if user exists or not
                    return Unauthorized("Invalid username or password.");
                }

                // 2. Verify password
                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed for username '{Username}'. Invalid password.", model.Username);
                    return Unauthorized("Invalid username or password.");
                }

                // 3. Update LastLoginAt (optional)
                user.LastLoginAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // 4. Generate JWT Token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User '{Username}' logged in successfully.", user.Username);
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for username '{Username}'.", model.Username);
                return StatusCode(500, "An internal error occurred during login.");
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseModel>> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Check if username or email already exists
                if (await _userRepository.UserExistsAsync(model.Username, model.Email))
                {
                    ModelState.AddModelError("", "Username or email already exists.");
                    return BadRequest(ModelState);
                }

                // 2. Find or create brand
                Brand? brand = await _brandRepository.GetByNameAsync(model.BrandName);
                if (brand == null)
                {
                    // Create new brand if it doesn't exist
                    brand = new Brand { Name = model.BrandName };
                    await _brandRepository.AddAsync(brand);
                    await _brandRepository.SaveChangesAsync();
                    _logger.LogInformation("New brand '{BrandName}' created during user registration for user '{Username}'.", brand.Name, model.Username);
                }
                
                var freePlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(sp => sp.Name == "Free" && sp.IsActive);
                if (freePlan == null)
                {
                    _logger.LogError("Free subscription plan not found during registration for user '{Username}'.", model.Username);
                    return StatusCode(500, "Unable to assign subscription plan. Please contact support.");
                }
                
                var now = DateTime.UtcNow;
                var freeSubscription = new BrandSubscription
                {
                    BrandId = brand.Id,
                    SubscriptionPlanId = freePlan.Id,
                    StartDate = now,
                    EndDate = null, // Ongoing/free
                    Status = SubscriptionStatus.Active,
                    AmountPaid = 0m,
                    PaymentReference = "FREE_TRIAL", // Indicate it's a free plan
                    CodesGenerated = 0, // Start with zero usage
                    VerificationsReceived = 0
                };
                await _context.BrandSubscriptions.AddAsync(freeSubscription);
                await _context.SaveChangesAsync();
                
                // 6. Commit Transaction
                await transaction.CommitAsync();

                // 4. Link Brand to its new Subscription
                brand.CurrentSubscriptionId = freeSubscription.Id;
                _brandRepository.Update(brand);
                await _brandRepository.SaveChangesAsync();

                // 3. Create user
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    BrandId = brand.Id
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // 4. Generate JWT Token for the new user
                var token = GenerateJwtToken(user);

                _logger.LogInformation("New user '{Username}' registered successfully for brand '{BrandName}'.", user.Username, brand.Name);
                return CreatedAtAction(nameof(Login), token); // Or Ok(token)
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred during registration for username '{Username}'.", model.Username);
                return StatusCode(500, "An internal error occurred during registration.");
            }
        }

        private AuthResponseModel GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("BrandId", user.BrandId.ToString()), // Custom claim for BrandId
                    // Add roles if needed: new Claim(ClaimTypes.Role, "BrandAdmin")
                }),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"))),
                Issuer =  Environment.GetEnvironmentVariable("JWT_ISSUER"),
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AuthResponseModel
            {
                Token = tokenString,
                Expiration = tokenDescriptor.Expires ?? DateTime.UtcNow,
                UserId = user.Id,
                Username = user.Username,
                BrandId = user.BrandId,
                BrandName = user.Brand?.Name ?? "Unknown Brand" // Eager load Brand or fetch separately if needed
            };
        }
    }

    // Model for appsettings.json JWT section
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; }
    }

}
