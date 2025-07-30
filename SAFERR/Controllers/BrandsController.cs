using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAFERR.Entities;
using SAFERR.Extensions;
using SAFERR.Repositories;

namespace SAFERR.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly IBrandRepository _brandRepository;

        public BrandsController(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
        {
            var brands = await _brandRepository.GetAllAsync();
            return Ok(brands);
        }
        
        [HttpGet("my-brand")] // Route: GET /api/brands/my-brand
        [ProducesResponseType(typeof(Brand), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Brand>> GetMyBrand()
        {
            // 1. Get the authenticated user's BrandId from JWT claims
            var userBrandId = User.GetUserBrandId(); // Using the extension method

            if (!userBrandId.HasValue)
            {
                // This should ideally not happen if [Authorize] is working and JWT is correctly formed
                // _logger?.LogWarning("User claim 'BrandId' is missing for user {UserId}.", User.GetUserId());
                return NotFound("User brand association not found.");
            }

            // 2. Fetch the brand by its ID
            var brand = await _brandRepository.GetByIdAsync(userBrandId.Value);

            if (brand == null)
            {
                // This could happen if the brand was deleted but the user still has a reference
                // _logger?.LogWarning("Brand with ID {BrandId} associated with user not found.", userBrandId.Value);
                return NotFound("Associated brand not found.");
            }

            // 3. Return the brand
            return Ok(brand);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrand(Guid id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            return Ok(brand);
        }

        [HttpPost]
        public async Task<ActionResult<Brand>> CreateBrand(Brand brand)
        {
            var userBrandId = User.GetUserBrandId();

            if (userBrandId.HasValue)
            {
                // Optional: Fetch the existing brand to provide more details in the error message
                var existingBrand = await _brandRepository.GetByIdAsync(userBrandId.Value);
                var existingBrandName = existingBrand?.Name ?? "Unknown Brand";

                // _logger.LogWarning(
                //     "User {UserId} attempted to create a new brand '{NewBrandName}' but already has an associated brand '{ExistingBrandName}' (ID: {ExistingBrandId}).",
                //     User.GetUserId(), brand.Name, existingBrandName, userBrandId.Value);

                // Return Conflict (409) to indicate the resource (user-brand association) already exists
                // Or BadRequest (400) if you prefer, but 409 is more semantically correct for this scenario.
                return Conflict(new
                {
                    Message = "User already has an associated brand.",
                    ExistingBrandId = userBrandId.Value,
                    ExistingBrandName = existingBrandName,
                    Detail =
                        "The current policy allows only one brand per user. Contact support if you need to manage multiple brands."
                });
            }

            // Basic validation can be added here or via attributes
            if (string.IsNullOrWhiteSpace(brand.Name))
            {
                return BadRequest("Brand name is required.");
            }

            var brandExists = await _brandRepository.GetByNameAsync(brand.Name);

            if (brandExists != null)
            {
                return Conflict("Brand name already exists.");
            }
            

            await _brandRepository.AddAsync(brand);
            await _brandRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }
    }

}
