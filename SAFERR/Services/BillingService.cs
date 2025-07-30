// using System;
// using Microsoft.Extensions.Options;
// using Newtonsoft.Json.Linq;
// using PayStack.Net;
// using SAFERR.DTOs;
// using SAFERR.Entities;
// using SAFERR.Repositories;
// using Stripe;
// using Stripe.Checkout;
//
// namespace SAFERR.Services;
//
//     public class BillingService : IBillingService
//     {
//         private readonly IBrandRepository _brandRepository;
//         private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
//         private readonly IBrandSubscriptionRepository _brandSubscriptionRepository;
//         private readonly ILogger<BillingService> _logger;
//         private readonly PaystackSettings _paystackSettings;
//         private readonly PaystackTransaction _paystackTransaction; // Paystack SDK client
//
//         public BillingService(
//             IBrandRepository brandRepository,
//             ISubscriptionPlanRepository subscriptionPlanRepository,
//             IBrandSubscriptionRepository brandSubscriptionRepository,
//             ILogger<BillingService> logger,
//             IOptions<PaystackSettings> paystackOptions) // Inject Paystack settings
//         {
//             _brandRepository = brandRepository;
//             _subscriptionPlanRepository = subscriptionPlanRepository;
//             _brandSubscriptionRepository = brandSubscriptionRepository;
//             _logger = logger;
//             _paystackSettings = paystackOptions.Value;
//
//             // Initialize Paystack SDK client with secret key
//             if (!string.IsNullOrEmpty(_paystackSettings.SecretKey))
//             {
//                 _paystackTransaction = new PaystackTransaction(_paystackSettings.SecretKey);
//             }
//             else
//             {
//                 _logger.LogWarning("Paystack Secret Key is not configured. Paystack integration will be disabled.");
//             }
//         }
//
//         public async Task<string?> InitializeTransactionAsync(Guid brandId, Guid subscriptionPlanId, string callbackUrl)
//         {
//             var brand = await _brandRepository.GetByIdAsync(brandId);
//             var plan = await _subscriptionPlanRepository.GetByIdAsync(subscriptionPlanId);
//
//             if (brand == null || plan == null)
//             {
//                 _logger.LogWarning("InitializeTransactionAsync failed: Brand ({BrandId}) or Plan ({PlanId}) not found.", brandId, subscriptionPlanId);
//                 return null;
//             }
//
//             if (_paystackTransaction == null)
//             {
//                 _logger.LogError("Paystack SDK client is not initialized.");
//                 return null;
//             }
//
//             try
//             {
//                 // 1. Prepare transaction details
//                 // Paystack usually works with Kobo (1/100 of Naira). 
//                 // Assuming your price is in Naira, convert to Kobo.
//                 // If price is in cents/USD, adjust conversion logic accordingly.
//                 // Example assumes price is in Naira.
//                 int amountInKobo = (int)(plan.Price * 100); // Convert Naira to Kobo
//
//                 // 2. Create Paystack Transaction Initialization Request
//                 var request = new TransactionInitializeRequest
//                 {
//                     AmountInKobo = amountInKobo,
//                     Email = brand.ContactEmail ?? "noreply@example.com", // Paystack requires email
//                     Currency = "NGN", // Adjust currency as needed (Paystack supports NGN, GHS, ZAR)
//                     Reference = Guid.NewGuid().ToString(), // Generate a unique reference
//                     CallbackUrl = callbackUrl, // URL Paystack redirects user to after payment
//                     Metadata = new JObject // Use JObject for metadata
//                     {
//                         ["BrandId"] = brandId.ToString(),
//                         ["SubscriptionPlanId"] = subscriptionPlanId.ToString(),
//                         ["CustomFields"] = new JArray // Optional custom fields
//                         {
//                             new JObject { ["DisplayName"] = "Brand", ["VariableName"] = "brand_name", ["Value"] = brand.Name }
//                         }
//                     }
//                 };
//
//                 // 3. Call Paystack API to initialize transaction
//                 var response = await _paystackTransaction.InitializeAsync(request);
//
//                 if (response.Status && !string.IsNullOrEmpty(response.Data.AuthorizationUrl))
//                 {
//                     _logger.LogInformation(
//                         "Initialized Paystack transaction {Reference} for brand {BrandId} and plan {PlanId}. Amount: {Amount} {Currency}. Auth URL: {AuthUrl}",
//                         response.Data.Reference, brandId, subscriptionPlanId, amountInKobo / 100m, request.Currency, response.Data.AuthorizationUrl);
//
//                     // Optional: Store the Reference and initial state in your DB if needed before redirect
//                     // This can help track pending transactions.
//
//                     // Return the Authorization URL for the frontend to redirect the user
//                     return response.Data.AuthorizationUrl;
//                 }
//                 else
//                 {
//                     _logger.LogError("Failed to initialize Paystack transaction for brand {BrandId}. Message: {Message}", brandId, response.Message);
//                     return null;
//                 }
//
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An unexpected error occurred while initializing Paystack transaction for brand {BrandId} and plan {PlanId}.", brandId, subscriptionPlanId);
//                 return null;
//             }
//         }
//
//         public async Task<bool> HandlePaystackWebhookAsync(string json)
//         {
//             try
//             {
//                 // 1. Parse the JSON payload
//                 var jsonObject = JObject.Parse(json);
//                 var eventType = jsonObject["event"]?.ToString();
//
//                 _logger.LogDebug("Received Paystack webhook event: {EventType}", eventType);
//
//                 // 2. Handle relevant events, e.g., successful payment
//                 if (eventType == "charge.success") // Or "subscription.create" if using Paystack subscriptions
//                 {
//                     var data = jsonObject["data"] as JObject;
//                     if (data != null)
//                     {
//                         var status = data["status"]?.ToString();
//                         var reference = data["reference"]?.ToString();
//                         var amountInKobo = data["amount"]?.Value<int>() ?? 0;
//                         var currency = data["currency"]?.ToString();
//                         var metadata = data["metadata"] as JObject;
//
//                         if (status == "success" && !string.IsNullOrEmpty(reference))
//                         {
//                             // Extract metadata
//                             if (metadata != null &&
//                                 metadata.TryGetValue("BrandId", out var brandIdToken) &&
//                                 metadata.TryGetValue("SubscriptionPlanId", out var planIdToken) &&
//                                 Guid.TryParse(brandIdToken.ToString(), out Guid brandId) &&
//                                 Guid.TryParse(planIdToken.ToString(), out Guid planId))
//                             {
//                                 // 3. Fetch related entities
//                                 var brand = await _brandRepository.GetByIdAsync(brandId);
//                                 var plan = await _subscriptionPlanRepository.GetByIdAsync(planId);
//
//                                 if (brand != null && plan != null)
//                                 {
//                                     // 4. Create or update BrandSubscription record
//                                     var now = DateTime.UtcNow;
//                                     // Determine end date based on plan duration (you need to define this logic)
//                                     // Example: Assume annual plan based on price or a fixed duration property
//                                     // This is simplified; you might need a more robust way to determine duration
//                                     DateTime? endDate = now.AddYears(1); // Example: 1 year from now
//
//                                     var newSubscription = new BrandSubscription
//                                     {
//                                         BrandId = brandId,
//                                         SubscriptionPlanId = planId,
//                                         StartDate = now,
//                                         EndDate = endDate,
//                                         Status = SubscriptionStatus.Active,
//                                         AmountPaid = amountInKobo / 100m, // Convert back to main currency unit
//                                         PaymentReference = reference // Use Paystack reference
//                                     };
//
//                                     await _brandSubscriptionRepository.AddAsync(newSubscription);
//                                     await _brandSubscriptionRepository.SaveChangesAsync();
//
//                                     // 5. Update Brand's current subscription link
//                                     brand.CurrentSubscriptionId = newSubscription.Id;
//                                     _brandRepository.Update(brand);
//                                     await _brandRepository.SaveChangesAsync();
//
//                                     _logger.LogInformation(
//                                         "Processed successful charge.success webhook. Created BrandSubscription {SubId} for Brand {BrandId}. Reference: {Reference}",
//                                         newSubscription.Id, brandId, reference);
//                                     return true;
//                                 }
//                                 else
//                                 {
//                                     _logger.LogWarning("Webhook: Brand ({BrandId}) or Plan ({PlanId}) not found for transaction {Reference}.", brandId, planId, reference);
//                                 }
//                             }
//                             else
//                             {
//                                 _logger.LogWarning("Webhook: Missing or invalid metadata in charge.success for transaction {Reference}.", reference);
//                             }
//                         }
//                         else
//                         {
//                             _logger.LogWarning("Webhook: Transaction {Reference} was not successful (Status: {Status}).", reference, status);
//                         }
//                     }
//                 }
//                 // Handle other events like "charge.failed", "subscription.not_renewed" etc.
//                 // else if (eventType == "charge.failed") { ... }
//
//                 return true; // Acknowledge receipt of the event
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An unexpected error occurred while processing Paystack webhook.");
//                 return false; // Signal failure to process (though Paystack retries might depend on HTTP status)
//             }
//         }
//
//         public async Task<bool> VerifyTransactionAsync(string transactionReference)
//         {
//              if (_paystackTransaction == null)
//             {
//                 _logger.LogError("Paystack SDK client is not initialized for verification.");
//                 return false;
//             }
//
//             try
//             {
//                 var response = await _paystackTransaction.VerifyTransaction(transactionReference);
//
//                 if (response.Status && response.Data?.Status == "success")
//                 {
//                     _logger.LogInformation("Transaction {Reference} verified successfully.", transactionReference);
//                     return true;
//                 }
//                 else
//                 {
//                     _logger.LogWarning("Transaction {Reference} verification failed or was not successful. Message: {Message}", transactionReference, response.Message);
//                     return false;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "An error occurred while verifying Paystack transaction {Reference}.", transactionReference);
//                 return false;
//             }
//         }
//
//
//         // Implement CancelSubscriptionAsync if needed for manual logic
//         public Task<bool> CancelSubscriptionAsync(Guid brandSubscriptionId)
//         {
//              // Paystack doesn't have a direct API for cancelling a "one-time payment" subscription model.
//             // You would implement your own logic in BrandSubscription (e.g., setting Status to Cancelled)
//             // and potentially notifying the brand.
//             // If you use Paystack's Subscription feature, there would be an API call.
//              _logger.LogWarning("CancelSubscriptionAsync called for BrandSubscription {SubId}. Implement custom cancellation logic or use Paystack Subscriptions API if applicable.", brandSubscriptionId);
//              return Task.FromResult(false); // Placeholder
//         }
//     }
//
//
