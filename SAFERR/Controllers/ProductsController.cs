using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAFERR.DTOs;
using SAFERR.Entities;
using SAFERR.Repositories;

namespace SAFERR.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly IBrandRepository _brandRepository; // To check if brand exists

        public ProductsController(IProductRepository productRepository, IBrandRepository brandRepository)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpGet("by-brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByBrand(Guid brandId)
        {
            // Optional: Check if brand exists
            // var brand = await _brandRepository.GetByIdAsync(brandId);
            // if (brand == null) return NotFound("Brand not found.");

            var products = await _productRepository.GetByBrandIdAsync(brandId);
            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductDto product)
        {

            var savedProduct = new Product()
            {
                Id = Guid.NewGuid(),
                BrandId = product.BrandId,
                Description = product.Description,
                Identifier = product.Identifier,
                Name = product.Name,
            };
            // Basic validation
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return BadRequest("Product name is required.");
            }

            // Check if Brand exists
            var brand = await _brandRepository.GetByIdAsync(product.BrandId);
            if (brand == null)
            {
                return BadRequest("Invalid BrandId. Brand not found.");
            }

            await _productRepository.AddAsync(savedProduct);
            await _productRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = savedProduct.Id }, product);
        }

        // PUT, DELETE endpoints can be added similarly
    }

}
