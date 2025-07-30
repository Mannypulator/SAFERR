using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAFERR.Entities;
using SAFERR.Extensions;
using SAFERR.Repositories;

namespace SAFERR.Controllers
{
    // TODO: Add [Authorize] for brand authentication
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IBrandSubscriptionRepository _brandSubscriptionRepository;
        private readonly IBrandRepository _brandRepository;
        // private readonly IBillingService _billingService; // If implemented

        public SubscriptionsController(
            ISubscriptionPlanRepository subscriptionPlanRepository,
            IBrandSubscriptionRepository brandSubscriptionRepository,
            IBrandRepository brandRepository
            // IBillingService billingService // If implemented
            )
        {
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _brandSubscriptionRepository = brandSubscriptionRepository;
            _brandRepository = brandRepository;
            // _billingService = billingService;
        }

        /// <summary>
        /// Gets all active subscription plans.
        /// </summary>
        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlan>>> GetActivePlans()
        {
            var plans = await _subscriptionPlanRepository.GetActivePlansAsync();
            return Ok(plans);
        }

        /// <summary>
        /// Gets the current subscription for a specific brand.
        /// </summary>
        /// <param name="brandId">The ID of the brand.</param>
        [HttpGet("current")]
        public async Task<ActionResult<BrandSubscription>> GetCurrentSubscription()
        {
            var userBrandId = User.GetUserBrandId();
            if (!userBrandId.HasValue)
            {
                return Forbid("User brand association not found.");
            }
            var subscription = await _brandSubscriptionRepository.GetCurrentSubscriptionForBrandAsync(userBrandId.Value);
            if (subscription == null)
            {
                return NotFound("No active subscription found for this brand.");
            }
            return Ok(subscription);
        }

        /// <summary>
        /// Gets the subscription history for a specific brand.
        /// </summary>
        /// <param name="brandId">The ID of the brand.</param>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<BrandSubscription>>> GetSubscriptionHistory()
        {
            var userBrandId = User.GetUserBrandId();
            if (!userBrandId.HasValue)
            {
                return Forbid("User brand association not found.");
            }
            var history = await _brandSubscriptionRepository.GetSubscriptionHistoryForBrandAsync(userBrandId.Value);
            return Ok(history);
        }

        /*
        /// <summary>
        /// Initiates the subscription checkout process with Stripe.
        /// </summary>
        /// <param name="request">The checkout request details.</param>
        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession([FromBody] CheckoutRequest request)
        {
            // In a real implementation, authenticate the brand making the request
            // var brandId = GetAuthenticatedBrandId(); // Implement this

            var sessionId = await _billingService.CreateCheckoutSessionAsync(
                request.BrandId, // Or authenticated brand ID
                request.SubscriptionPlanId,
                request.SuccessUrl,
                request.CancelUrl);

            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Failed to create checkout session.");
            }

            return Ok(new CheckoutSessionResponse { SessionId = sessionId });
        }

        public class CheckoutRequest
        {
            public Guid BrandId { get; set; } // Or get from auth context
            public Guid SubscriptionPlanId { get; set; }
            public string SuccessUrl { get; set; } = string.Empty;
            public string CancelUrl { get; set; } = string.Empty;
        }

        public class CheckoutSessionResponse
        {
            public string SessionId { get; set; } = string.Empty;
        }
        */


        // --- ADMIN/INTERNAL ENDPOINTS (Not exposed to brands directly) ---
        // Endpoints for creating plans, manually assigning subscriptions, handling Stripe webhooks
        // would typically be in a separate Admin controller or secured differently.

        /// <summary>
        /// (Admin) Creates a new subscription plan.
        /// </summary>
        [HttpPost("plans")] // Secure this endpoint appropriately
        public async Task<ActionResult<SubscriptionPlan>> CreatePlan([FromBody] SubscriptionPlan plan)
        {
            // Add validation
            if (string.IsNullOrWhiteSpace(plan.Name))
            {
                return BadRequest("Plan name is required.");
            }

            await _subscriptionPlanRepository.AddAsync(plan);
            await _subscriptionPlanRepository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetActivePlans), new { id = plan.Id }, plan);
        }

        /// <summary>
        /// (Admin/Manual Process) Assigns a subscription to a brand.
        /// This bypasses payment and is for manual setup/billing.
        /// </summary>
        [HttpPost("assign")] // Secure this endpoint appropriately
        public async Task<ActionResult<BrandSubscription>> AssignSubscription([FromBody] AssignSubscriptionRequest request)
        {
            var brand = await _brandRepository.GetByIdAsync(request.BrandId);
            var plan = await _subscriptionPlanRepository.GetByIdAsync(request.SubscriptionPlanId);

            if (brand == null || plan == null)
            {
                return BadRequest("Invalid brand or subscription plan ID.");
            }

            var now = DateTime.UtcNow;
            var endDate = request.SubscriptionLengthMonths > 0 ?
                          now.AddMonths(request.SubscriptionLengthMonths) :
                          (DateTime?)null; // Null for indefinite

            var newSubscription = new BrandSubscription
            {
                BrandId = request.BrandId,
                SubscriptionPlanId = request.SubscriptionPlanId,
                StartDate = now,
                EndDate = endDate,
                Status = SubscriptionStatus.Active,
                AmountPaid = request.AmountPaid,
                PaymentReference = request.PaymentReference
            };

            await _brandSubscriptionRepository.AddAsync(newSubscription);
            await _brandSubscriptionRepository.SaveChangesAsync();

            // Update Brand's current subscription link
            brand.CurrentSubscriptionId = newSubscription.Id;
            _brandRepository.Update(brand);
            await _brandRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCurrentSubscription), new { brandId = request.BrandId }, newSubscription);
        }

        public class AssignSubscriptionRequest
        {
            public Guid BrandId { get; set; }
            public Guid SubscriptionPlanId { get; set; }
            public int SubscriptionLengthMonths { get; set; } // 0 for indefinite
            public decimal AmountPaid { get; set; }
            public string? PaymentReference { get; set; }
        }
        // -------------------------------------------------------------------
    }

}
