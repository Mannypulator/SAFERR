using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAFERR.DTOs;
using SAFERR.Services;

namespace SAFERR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityCodesController : ControllerBase
    {
        private readonly ISecurityCodeService _securityCodeService;
        private readonly ILogger<SecurityCodesController> _logger;

        public SecurityCodesController(ISecurityCodeService securityCodeService, ILogger<SecurityCodesController> logger)
        {
            _securityCodeService = securityCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a batch of unique codes for a specific product.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="count">The number of codes to generate.</param>
        /// <returns>A list of generated codes.</returns>
        [HttpPost("generate/{productId}")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GenerateCodes(Guid productId, [FromQuery, Range(1, 10000)] int count)
        {
            try
            {
                var codes = await _securityCodeService.GenerateCodesForProductAsync(productId, count);
                return Ok(new { GeneratedCodes = codes, Count = codes.Count });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Product not found"))
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex) when (ex.ParamName == "count")
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to generate codes for product {ProductId}", productId);
                return StatusCode(500, "An error occurred while generating codes.");
            }
        }

        /// <summary>
        /// Verifies a product code. Simulates receiving an SMS.
        /// In a real scenario, this might be triggered by a background service listening to an SMS gateway.
        /// </summary>
        /// <param name="code">The code to verify.</param>
        /// <param name="sourcePhoneNumber">Optional: The phone number that sent the SMS.</param>
        /// <returns>The verification result.</returns>
        [HttpPost("verify")]
        public async Task<ActionResult<VerificationResultDto>> VerifyCode([FromBody] VerifyCodeRequest request)
        {
            // In a real SMS scenario, source IP might not be directly available from SMS,
            // but could be from the SMS gateway's API call metadata.
            // For this API endpoint, we take it as a parameter if provided.
            var sourceIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var result = await _securityCodeService.VerifyCodeAsync(
                    request.Code?.Trim() ?? "",
                    request.SourcePhoneNumber?.Trim(),
                    sourceIpAddress
                );
                return Ok(result);
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "An unexpected error occurred during code verification for code '{CodeInput}'", request.Code);
                return StatusCode(500, "An internal error occurred during verification.");
            }
        }

        /// <summary>
        /// Gets basic statistics about code generation and verification.
        /// </summary>
        /// <returns>Statistics DTO.</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<SecurityCodeStatsDto>> GetStatistics()
        {
            try
            {
                var stats = await _securityCodeService.GetStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve statistics.");
                return StatusCode(500, "An error occurred while retrieving statistics.");
            }
        }
    }

   

}
