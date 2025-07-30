using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAFERR.DTOs;
using SAFERR.Extensions;
using SAFERR.Services;

namespace SAFERR.Controllers
{
    // TODO: Add [Authorize] attribute to secure these endpoints for brand users only
    [Authorize] // Ensure only authenticated users can access reports
    [ApiController]
    [Route("api/[controller]")] // Base route will be /api/reporting
    public class ReportingController : ControllerBase
    {
        private readonly ISecurityCodeService _securityCodeService;
        private readonly ILogger<ReportingController> _logger;

        public ReportingController(ISecurityCodeService securityCodeService, ILogger<ReportingController> logger)
        {
            _securityCodeService = securityCodeService;
            _logger = logger;
        }

        /// <summary>
        /// Gets verification trends over a specified date range for the authenticated user's brand.
        /// </summary>
        /// <param name="startDate">The start date (inclusive).</param>
        /// <param name="endDate">The end date (inclusive).</param>
        /// <returns>Verification trend data for the brand.</returns>
        [HttpGet("trends")]
        public async Task<ActionResult<VerificationTrendDto>> GetVerificationTrend(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var userBrandId = User.GetUserBrandId();
            if (!userBrandId.HasValue)
            {
                return Forbid("User brand association not found.");
            }

            if (startDate > endDate)
            {
                return BadRequest("Start date must be before or equal to end date.");
            }

            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest("Date range cannot exceed 365 days.");
            }

            try
            {
                var trendData =
                    await _securityCodeService.GetVerificationTrendForBrandAsync(userBrandId.Value, startDate, endDate);
                return Ok(trendData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching verification trends for brand {BrandId} from {StartDate} to {EndDate}",
                    userBrandId.Value, startDate, endDate);
                return StatusCode(500, "An error occurred while fetching verification trends.");
            }
        }

        /// <summary>
        /// Gets a list of potentially suspicious activities (codes verified multiple times) for the authenticated user's brand.
        /// </summary>
        /// <param name="limit">The maximum number of activities to return (default 10).</param>
        /// <returns>A list of suspicious activities for the brand.</returns>
        [HttpGet("suspicious")]
        public async Task<ActionResult<IEnumerable<SuspiciousActivityDto>>> GetSuspiciousActivities(
            [FromQuery] int limit = 10)
        {
            var userBrandId = User.GetUserBrandId();
            if (!userBrandId.HasValue)
            {
                return Forbid("User brand association not found.");
            }

            if (limit <= 0 || limit > 100)
            {
                return BadRequest("Limit must be between 1 and 100.");
            }

            try
            {
                var activities =
                    await _securityCodeService.GetSuspiciousActivitiesForBrandAsync(userBrandId.Value, limit);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suspicious activities for brand {BrandId} with limit {Limit}",
                    userBrandId.Value, limit);
                return StatusCode(500, "An error occurred while fetching suspicious activities.");
            }
        }

        /// <summary>
        /// Gets insights into product verification distribution for the authenticated user's brand.
        /// </summary>
        /// <returns>Product distribution data for the brand.</returns>
        [HttpGet("product-distribution")]
        public async Task<ActionResult<ProductDistributionDto>> GetProductDistribution()
        {
            var userBrandId = User.GetUserBrandId();
            if (!userBrandId.HasValue)
            {
                return Forbid("User brand association not found.");
            }

            try
            {
                var distribution = await _securityCodeService.GetProductDistributionForBrandAsync(userBrandId.Value);
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product distribution for brand {BrandId}.", userBrandId.Value);
                return StatusCode(500, "An error occurred while fetching product distribution data.");
            }
        }
    }

}
