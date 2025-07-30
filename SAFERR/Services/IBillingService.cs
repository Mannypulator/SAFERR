using System;

namespace SAFERR.Services;

public interface IBillingService
{
    public interface IBillingService
    {
        /// <summary>
        /// Creates a Paystack Transaction/Authorization URL for a brand to subscribe to a plan.
        /// (Note: Paystack doesn't have built-in recurring billing like Stripe. You might need to
        /// implement recurring logic manually or use Paystack Subscriptions if available in your region/plan).
        /// For simplicity, this example treats it as a one-time payment for plan access for a period.
        /// </summary>
        /// <param name="brandId">The ID of the brand subscribing.</param>
        /// <param name="subscriptionPlanId">The ID of the plan being subscribed to.</param>
        /// <param name="callbackUrl">The URL Paystack redirects to after payment attempt.</param>
        /// <returns>The Paystack Authorization URL, or null if creation failed.</returns>
        Task<string?> InitializeTransactionAsync(Guid brandId, Guid subscriptionPlanId, string callbackUrl);

        /// <summary>
        /// Handles incoming webhooks from Paystack (e.g., payment success, payment failure).
        /// </summary>
        /// <param name="json">The raw JSON payload from Paystack.</param>
        /// <returns>True if the webhook was processed successfully, otherwise false.</returns>
        Task<bool> HandlePaystackWebhookAsync(string json);

        /// <summary>
        /// (Optional) Verifies a transaction with Paystack to confirm its status.
        /// </summary>
        /// <param name="transactionReference">The reference ID of the transaction.</param>
        /// <returns>True if verified and successful, otherwise false.</returns>
        Task<bool> VerifyTransactionAsync(string transactionReference);

        // Consider if you need explicit cancel/expire logic for your manual subscription model
    }
}
