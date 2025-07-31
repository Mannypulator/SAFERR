using Microsoft.Extensions.Options;
using SAFERR.DTOs;
using SAFERR.Entities;
using SAFERR.Repositories;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SAFERR.Services;

public class SecurityCodeService : ISecurityCodeService
{
    private readonly ISecurityCodeRepository _securityCodeRepository;

    // private readonly TwilioSettings _twilioSettings;
    private readonly IBrandRepository _brandRepository; // Add if not already present
    private readonly IBrandSubscriptionRepository _brandSubscriptionRepository; // Add
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IVerificationLogRepository _verificationLogRepository;
    private readonly IProductRepository _productRepository; // To get product/brand details for response
    private readonly Serilog.ILogger _logger;
    private static readonly Random Random = new Random(); // Consider using a cryptographically secure RNG in production
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // 62 characters
    private const int CodeLength = 12; // Adjust as needed, but 12 chars gives 62^12 possibilities >> 62 trillion

    public SecurityCodeService(
        ISecurityCodeRepository securityCodeRepository,
        IVerificationLogRepository verificationLogRepository,
        IProductRepository productRepository,
        Serilog.ILogger logger,
        // IOptions<TwilioSettings> twilioOptions,
        IBrandRepository brandRepository,
        IBrandSubscriptionRepository brandSubscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository)
    {
        _securityCodeRepository = securityCodeRepository;
        _verificationLogRepository = verificationLogRepository;
        _productRepository = productRepository;
        _logger = logger.ForContext<SecurityCodeService>();
        // _twilioSettings = twilioOptions.Value;
        _brandRepository = brandRepository;
        _brandSubscriptionRepository = brandSubscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
    }

    /// <summary>
    /// Generates a single unique alphanumeric code.
    /// </summary>
    /// <returns>A unique code string.</returns>
    public async Task<string> GenerateUniqueCodeAsync()
    {
        string code;
        int attempts = 0;
        const int maxAttempts = 10; // Prevent infinite loops

        do
        {
            code = GenerateRandomCode(CodeLength);
            attempts++;

            if (attempts > maxAttempts)
            {
                // Log error or throw exception if max attempts reached
                _logger.Error("Failed to generate a unique code after {MaxAttempts} attempts.", maxAttempts);
                throw new InvalidOperationException("Unable to generate a unique code after multiple attempts.");
            }
        } while (await _securityCodeRepository.CodeExistsAsync(code));

        _logger.Information("Generated unique code: {Code}", code);
        return code;
    }

    /// <summary>
    /// Generates multiple unique codes for a specific product.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="count">The number of codes to generate.</param>
    /// <returns>A list of generated unique code strings.</returns>
    public async Task<List<string>> GenerateCodesForProductAsync(Guid productId, int count)
    {
        _logger.Information("Initiating generation of {Count} codes for Product ID {ProductId}.", count, productId);

        // 1. Basic Validation
        if (count <= 0)
        {
            var errorMsg = "Code generation count must be greater than zero.";
            _logger.Information("Invalid request: {ErrorMessage} for Product ID {ProductId}.", errorMsg, productId);
            throw new ArgumentException(errorMsg, nameof(count));
        }

        if (count > 10000) // Example hard limit, adjust as needed
        {
            var errorMsg = "Maximum code generation count is 10,000 per request.";
            _logger.Information("Invalid request: {ErrorMessage} for Product ID {ProductId}.", errorMsg, productId);
            throw new ArgumentException(errorMsg, nameof(count));
        }

        // 2. Fetch and Validate Product & Brand Association
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            var errorMsg = "Product not found for code generation.";
            _logger.Information(errorMsg + " Product ID: {ProductId}.", productId);
            throw new ArgumentException(errorMsg, nameof(productId));
        }

        // 3. Subscription Check (Placeholder - implement based on your subscription logic)
        // This part depends on how subscription limits are enforced.
        // Example: Check if the brand associated with the product has quota.
        // var (isAllowed, message) = await CheckCodeGenerationPermissionAsync(product.BrandId, count);
        // if (!isAllowed)
        // {
        //     _logger.LogWarning(
        //         "Subscription check failed for code generation. Brand ID: {BrandId}, Product ID: {ProductId}, Count: {Count}. Reason: {Reason}",
        //         product.BrandId, productId, count, message);
        //     throw new InvalidOperationException($"Subscription check failed: {message}");
        // }
        // _logger.LogDebug("Subscription check passed for Brand ID {BrandId}.", product.BrandId);

        // 4. --- Core Code Generation Logic ---
        var generatedCodes = new List<string>();
        var securityCodesToInsert = new List<SecurityCode>(); // List to hold entities for DB insertion
        var failedAttempts = 0;
        int maxFailedAttemptsPerBatch = count * 10; // Reasonable buffer for collision retries

        // Use a simple loop with retry logic for uniqueness
        while (generatedCodes.Count < count && failedAttempts < maxFailedAttemptsPerBatch)
        {
            var code = GenerateRandomCode(CodeLength);

            // Check for uniqueness in the CURRENT BATCH being generated *AND* in the DATABASE
            // Using repository method for DB check is important.
            if (!generatedCodes.Contains(code) && !await _securityCodeRepository.CodeExistsAsync(code))
            {
                generatedCodes.Add(code);
                // Create the SecurityCode entity to be saved
                var securityCodeEntity = new SecurityCode
                {
                    Code = code,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                    // IsApplied, IsVerified, FirstVerifiedAt default to false/DateTime.Min
                };
                securityCodesToInsert.Add(securityCodeEntity); // Add to list for bulk insert
            }
            else
            {
                failedAttempts++;
                _logger.Information(
                    "Code generation collision detected. Failed attempts: {FailedAttempts}/{MaxAttempts}",
                    failedAttempts, maxFailedAttemptsPerBatch);
            }
        }

        if (generatedCodes.Count != count)
        {
            _logger.Error(
                "Failed to generate {RequestedCount} unique codes for product {ProductId} after {FailedAttempts} collisions.",
                count, productId, failedAttempts);
            throw new InvalidOperationException(
                $"Could not generate {count} unique codes due to excessive collisions.");
        }
        // --- End Core Code Generation Logic ---

        // 5. --- Persist Generated Codes to Database ---
        try
        {
            // Use AddRangeAsync for efficient bulk insertion if supported by your repository
            // Otherwise, loop and use AddAsync, but AddRangeAsync is preferred.
            await _securityCodeRepository.AddRangeAsync(securityCodesToInsert);

            // IMPORTANT: This is the crucial step that was likely missing or incorrectly implemented before.
            // SaveChangesAsync commits the added entities to the database.
            await _securityCodeRepository.SaveChangesAsync();

            _logger.Information("Successfully generated and stored {Count} codes for Product ID {ProductId}.", count,
                productId);
            return generatedCodes;
        }
        catch (Exception ex) // Catch database errors, etc.
        {
            _logger.Error(ex,
                "An error occurred while saving {Count} generated codes for Product ID {ProductId} to the database.",
                count, productId);
            // Consider: Should partially generated codes be usable if save fails?
            // Current approach: Fail the whole operation if persistence fails.
            throw new InvalidOperationException(
                "Codes were generated but failed to save to the database. Please try again.", ex);
        }
    }


    /// <summary>
    /// Verifies a code provided by the user (e.g., via SMS).
    /// </summary>
    /// <param name="codeInput">The code sent by the user.</param>
    /// <param name="sourcePhoneNumber">Optional: Source phone number for logging.</param>
    /// <param name="sourceIpAddress">Optional: Source IP address for logging.</param>
    /// <returns>The verification result.</returns>
    public async Task<VerificationResultDto> VerifyCodeAsync(string codeInput, string? sourcePhoneNumber = null,
        string? sourceIpAddress = null)
    {
        // --- Performance: Start timing ---
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string? logCodeId = null; // Capture for logging duration
        // --------------------------------

        _logger.Information(
            "Processing verification request. Code Input: '{CodeInput}', Source Phone: {SourcePhone}, Source IP: {SourceIp}",
            codeInput, sourcePhoneNumber ?? "N/A", sourceIpAddress ?? "N/A");

        var resultDto = new VerificationResultDto
        {
            Result = VerificationResult.Error,
            Message = "An error occurred during verification."
        };

        if (string.IsNullOrWhiteSpace(codeInput))
        {
            resultDto.Result = VerificationResult.InvalidCodeFormat;
            resultDto.Message = "Invalid code format.";
            _logger.Warning("Verification failed due to invalid code format. Input: '{CodeInput}'.", codeInput);
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress, null);
            stopwatch.Stop();
            _logger.Debug("Verification process (Invalid Format) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }

        SecurityCode? securityCode = null;
        try
        {
            // --- Performance: Direct DB Call (if repository method isn't optimized) ---
            // Ensure _securityCodeRepository.GetByCodeValueAsync is efficient (uses index)
            securityCode = await _securityCodeRepository.GetByCodeValueAsync(codeInput.Trim());
            // ------------------------------------------------------------------------
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Database error occurred while looking up code '{CodeInput}'.", codeInput);
            resultDto.Message = "A temporary error occurred. Please try again.";
            await LogVerificationAttempt(codeInput, VerificationResult.Error, sourcePhoneNumber, sourceIpAddress, null);
            stopwatch.Stop();
            _logger.Debug("Verification process (DB Error) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }

        if (securityCode == null)
        {
            resultDto.Result = VerificationResult.Counterfeit;
            resultDto.Message = "This code is not recognized. The product may be counterfeit.";
            _logger.Information("Verification result: Counterfeit. Code '{CodeInput}' not found.", codeInput);
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress, null);
            stopwatch.Stop();
            _logger.Debug("Verification process (Counterfeit) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }

        logCodeId = securityCode.Id.ToString(); // Capture ID for duration log

        // --- Subscription Check (now cached) ---
        Guid brandIdForVerification = securityCode.Product?.BrandId ?? Guid.Empty;
        if (brandIdForVerification == Guid.Empty)
        {
            _logger.Warning("Security code {CodeId} is not associated with a valid product/brand.", securityCode.Id);
            resultDto.Result = VerificationResult.Error;
            resultDto.Message = "Unable to verify subscription status for this product. Please contact support.";
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress,
                securityCode.Id);
            stopwatch.Stop();
            _logger.Debug("Verification process (No Brand Link) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }

        var (isAllowed, message) = await CheckVerificationPermissionAsync(brandIdForVerification);
        if (!isAllowed)
        {
            _logger.Warning(
                "Verification blocked due to subscription limit. Code: '{CodeInput}', Brand ID: {BrandId}. Reason: {Reason}",
                codeInput, brandIdForVerification, message);
            resultDto.Result = VerificationResult.Error;
            resultDto.Message = $"Verification blocked: {message}";
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress,
                securityCode.Id);
            stopwatch.Stop();
            _logger.Debug("Verification process (Subscription Blocked) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }
        // ---------------------------------------

        // ... existing verification logic (checking IsVerified, etc.) ...

        try
        {
            // ... verification logic ...

            if (securityCode.IsVerified)
            {
                // ... AlreadyVerified logic ...
                _logger.Information(
                    "Verification result: Already Verified. Code ID: {CodeId}, Product ID: {ProductId}, Brand ID: {BrandId}.",
                    securityCode.Id, securityCode.ProductId, brandIdForVerification);
            }
            else
            {
                // ... Genuine logic ...
                _logger.Information(
                    "Verification result: Genuine. Code ID: {CodeId}, Product ID: {ProductId}, Brand ID: {BrandId}.",
                    securityCode.Id, securityCode.ProductId, brandIdForVerification);

                // ... update code ...
                await _securityCodeRepository.SaveChangesAsync();
            }

            // ... log verification attempt ...
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress,
                securityCode.Id);

            // --- Usage Update (now cached) ---
            if (resultDto.Result == VerificationResult.Genuine ||
                resultDto.Result == VerificationResult.AlreadyVerified)
            {
                // The repository's UpdateUsageAsync now handles cache invalidation
                var currentSubscription =
                    await _brandSubscriptionRepository.GetCurrentSubscriptionForBrandAsync(brandIdForVerification);
                if (currentSubscription != null)
                {
                    await _brandSubscriptionRepository.UpdateUsageAsync(currentSubscription.Id,
                        verificationsReceivedDelta: 1);
                }
            }
            // ----------------------------------

            stopwatch.Stop();
            _logger.Debug(
                "Verification process (Success: {Result}) for Code ID {CodeId} took {ElapsedMilliseconds} ms.",
                resultDto.Result, logCodeId, stopwatch.ElapsedMilliseconds);

            // --- Performance Check ---
            if (stopwatch.ElapsedMilliseconds > 3000) // Log if approaching 5s SLA
            {
                _logger.Warning(
                    "Slow verification detected. Result: {Result}, Code ID: {CodeId}, Duration: {ElapsedMilliseconds} ms.",
                    resultDto.Result, logCodeId, stopwatch.ElapsedMilliseconds);
            }
            // -------------------------

            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "An error occurred while finalizing verification for code '{CodeInput}' (Code ID: {CodeId}).",
                codeInput, securityCode.Id);
            resultDto.Result = VerificationResult.Error;
            resultDto.Message = "An error occurred while processing your verification. Please try again.";
            await LogVerificationAttempt(codeInput, resultDto.Result, sourcePhoneNumber, sourceIpAddress,
                securityCode.Id);

            stopwatch.Stop();
            _logger.Debug("Verification process (Finalization Error) took {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
            return resultDto;
        }
    }


    /// <summary>
    /// Gets basic statistics about codes and verifications.
    /// </summary>
    /// <returns>Statistics DTO.</returns>
    public async Task<SecurityCodeStatsDto> GetStatisticsAsync()
    {
        var stats = new SecurityCodeStatsDto
        {
            TotalCodesGenerated = await _securityCodeRepository.GetTotalCountAsync(),
            TotalCodesVerified = await _securityCodeRepository.GetVerifiedCountAsync(),
            TotalVerificationsAttempted = await _verificationLogRepository.GetTotalVerificationsAsync(),
            VerificationResults = await _verificationLogRepository.GetVerificationResultCountsAsync()
        };
        return stats;
    }

    public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")))
        {
            _logger.Warning("Twilio settings are missing. Cannot send SMS to {ToPhoneNumber}.", toPhoneNumber);
            return false;
        }

        try
        {
            // Initialize Twilio client (can also be done globally in Program.cs as shown before)
            // TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")), // Your Twilio number
                to: new PhoneNumber(toPhoneNumber) // Recipient's number (e.g., from verification log)
            );

            if (messageResource.Status.ToString().Equals("sent", StringComparison.OrdinalIgnoreCase) ||
                messageResource.Status.ToString().Equals("queued", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Information("SMS sent successfully to {ToPhoneNumber}. SID: {MessageSid}", toPhoneNumber,
                    messageResource.Sid);
                return true;
            }
            else
            {
                _logger.Warning(
                    "SMS sending failed or pending for {ToPhoneNumber}. Status: {Status}, ErrorCode: {ErrorCode}",
                    toPhoneNumber, messageResource.Status, messageResource.ErrorCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while sending SMS to {ToPhoneNumber}", toPhoneNumber);
            return false;
        }
    }

    public async Task<VerificationTrendDto> GetVerificationTrendAsync(DateTime startDate, DateTime endDate)
    {
        var logsInPeriod = await _verificationLogRepository
            .FindAsync(v => v.VerificationAttemptedAt >= startDate && v.VerificationAttemptedAt <= endDate);

        var dailyCounts = logsInPeriod
            .GroupBy(log => log.VerificationAttemptedAt.Date)
            .Select(g => new DailyVerificationCount
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(dvc => dvc.Date)
            .ToList();

        return new VerificationTrendDto
        {
            DailyCounts = dailyCounts,
            TotalVerifications = dailyCounts.Sum(d => d.Count)
        };
    }

    // public async Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesAsync(int limit = 10)
    // {
    //     // Suspicious activity: Codes verified more than once
    //     // This query is a bit complex, best done directly with the context or a specialized repo method
    //     // We'll add a method to the VerificationLogRepository for this.
    //
    //     var suspiciousLogs = await _verificationLogRepository.GetSuspiciousActivitiesAsync(limit);
    //
    //     // Map to DTO (assuming GetSuspiciousActivitiesAsync returns a suitable anonymous/DTO type or we adjust it)
    //     // For now, assume the repository method returns the DTO directly or we map it here.
    //     // Let's refine the repository method first.
    //     return suspiciousLogs; // Assuming correctly mapped by repo method
    // }

    // public async Task<ProductDistributionDto> GetProductDistributionAsync()
    // {
    //     // Get top products by verification count
    //     // Join VerificationLog -> SecurityCode -> Product -> Brand
    //     // Group by Product, count verifications
    //
    //     var topProducts = await _verificationLogRepository.GetTopVerifiedProductsAsync(10); // Get top 10
    //
    //     return new ProductDistributionDto
    //     {
    //         TopProducts = topProducts.ToList(),
    //         TotalProducts = await _productRepository.CountAsync() // Total distinct products in the system
    //     };
    // }

    public async Task<(bool IsAllowed, string Message)> CheckCodeGenerationPermissionAsync(Guid brandId,
        int requestedCount)
    {
        var brand = await _brandRepository.GetByIdAsync(brandId);
        if (brand == null)
        {
            return (false, "Brand not found.");
        }

        var currentSubscription = await _brandSubscriptionRepository.GetCurrentSubscriptionForBrandAsync(brandId);

        if (currentSubscription == null || currentSubscription.Status != SubscriptionStatus.Active)
        {
            return (false, "No active subscription found for this brand.");
        }

        var plan = currentSubscription.SubscriptionPlan;
        if (plan == null)
        {
            _logger.Error("Subscription {SubscriptionId} for brand {BrandId} has no associated plan.",
                currentSubscription.Id, brandId);
            return (false, "Subscription plan details are missing. Please contact support.");
        }

        // Check code generation limit
        if (plan.MaxCodesPerMonth != -1 && // -1 means unlimited
            (currentSubscription.CodesGenerated + requestedCount) > plan.MaxCodesPerMonth)
        {
            return (false,
                $"Code generation limit exceeded. Your plan allows {plan.MaxCodesPerMonth} codes per month. You have generated {currentSubscription.CodesGenerated} so far.");
        }

        return (true, "Permission granted.");
    }

    public async Task<(bool IsAllowed, string Message)> CheckVerificationPermissionAsync(Guid brandId)
    {
        // This check might be less strict, or applied differently.
        // Verifications are usually a result of consumer action, not brand action.
        // However, we might want to track them against the brand's subscription limits.
        // For now, let's assume verifications are allowed if the brand has an active subscription.
        // The limit check could be done during reporting or billing cycle close.
        // Or, we can check it here too, similar to code generation.

        var brand = await _brandRepository.GetByIdAsync(brandId);
        if (brand == null)
        {
            // This check might not be directly applicable here as brandId comes from the verified SecurityCode
            // We'll pass brandId from the SecurityCode's Product.BrandId
            return (false, "Associated brand not found.");
        }

        var currentSubscription = await _brandSubscriptionRepository.GetCurrentSubscriptionForBrandAsync(brandId);

        if (currentSubscription == null || currentSubscription.Status != SubscriptionStatus.Active)
        {
            // Depending on policy, you might still allow verification but flag/log it
            // or prevent it. Let's prevent it for strict enforcement.
            return (false, "No active subscription found for the brand associated with this product.");
        }

        var plan = currentSubscription.SubscriptionPlan;
        if (plan == null)
        {
            _logger.Error("Subscription {SubscriptionId} for brand {BrandId} has no associated plan.",
                currentSubscription.Id, brandId);
            return (false, "Subscription plan details are missing for the associated brand.");
        }

        // Check verification limit (similar logic to code generation)
        if (plan.MaxVerificationsPerMonth != -1 && // -1 means unlimited
            (currentSubscription.VerificationsReceived + 1) > plan.MaxVerificationsPerMonth)
        {
            // Policy decision: Block the verification or just log/warn?
            // Let's block it for strict enforcement as per subscription model.
            return (false,
                $"Verification limit exceeded for the brand. The plan allows {plan.MaxVerificationsPerMonth} verifications per month. This limit has been reached.");
        }

        return (true, "Permission granted.");
    }


    #region Private Helpers

    private string GenerateRandomCode(int length)
    {
        var code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = Characters[Random.Next(Characters.Length)];
        }

        return new string(code);
    }

    private async Task LogVerificationAttempt(string codeAttempted, VerificationResult result,
        string? sourcePhoneNumber, string? sourceIpAddress, Guid? securityCodeId)
    {
        var logEntry = new VerificationLog
        {
            CodeAttempted = codeAttempted,
            VerificationAttemptedAt = DateTime.UtcNow,
            Result = result,
            SourcePhoneNumber = sourcePhoneNumber,
            SourceIpAddress = sourceIpAddress,
            SecurityCodeId = securityCodeId
        };

        await _verificationLogRepository.AddAsync(logEntry);
        await _verificationLogRepository.SaveChangesAsync(); // Assuming SaveChangesAsync is added
        _logger.Information("Logged verification attempt for code '{Code}' with result '{Result}'.", codeAttempted,
            result);
    }

    public async Task<VerificationTrendDto> GetVerificationTrendForBrandAsync(Guid brandId, DateTime startDate,
        DateTime endDate)
    {
        // Ensure endDate is at the end of the day
        endDate = endDate.Date.AddDays(1).AddTicks(-1);

        var logsInPeriod = await _verificationLogRepository.GetByDateRangeForBrandAsync(brandId, startDate, endDate);

        var dailyCounts = logsInPeriod
            .GroupBy(log => log.VerificationAttemptedAt.Date)
            .Select(g => new DailyVerificationCount
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(dvc => dvc.Date)
            .ToList();

        return new VerificationTrendDto
        {
            DailyCounts = dailyCounts,
            TotalVerifications = dailyCounts.Sum(d => d.Count)
        };
    }

    public async Task<IEnumerable<SuspiciousActivityDto>> GetSuspiciousActivitiesForBrandAsync(Guid brandId,
        int limit = 10)
    {
        // Delegate to the repository method
        return await _verificationLogRepository.GetSuspiciousActivitiesForBrandAsync(brandId, limit);
    }

    public async Task<ProductDistributionDto> GetProductDistributionForBrandAsync(Guid brandId)
    {
        // Delegate to the repository method
        var topProducts = await _verificationLogRepository.GetTopVerifiedProductsForBrandAsync(brandId, 10);

        // To get the total number of *distinct* products for the brand that have verifications,
        // we can count the distinct ProductIds from the topProducts list or query the DB.
        // Using the list is simpler if the limit is reasonable.
        var totalProductsWithVerifications = topProducts.Select(p => p.ProductId).Distinct().Count();

        // To get the *total* number of products for the brand (regardless of verification),
        // you would need a method on IProductRepository or IBrandRepository.
        // var totalProductsForBrand = await _productRepository.CountAsync(p => p.BrandId == brandId);
        // For now, let's just use the count of products with verifications.
        // If you need the total distinct products the brand has (even without verifications
        // showing in top 10), add a method to count them.

        return new ProductDistributionDto
        {
            TopProducts = topProducts.ToList(),
            TotalProducts = totalProductsWithVerifications // Or totalProductsForBrand if method added
        };
    }

    #endregion
}