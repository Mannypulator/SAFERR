using System;
using SAFERR.Services;

namespace SAFERR.BackgroundServices;

// Placeholder for an SMS listener service.
// This would typically integrate with an SMS Gateway provider's API (e.g., Twilio, Africa's Talking, etc.)
// to receive incoming SMS messages.
public class SmsListenerService : BackgroundService
{
    private readonly ILogger<SmsListenerService> _logger;
    private readonly IServiceProvider _serviceProvider; // To create scope for DI within the loop

    public SmsListenerService(ILogger<SmsListenerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SMS Listener Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // --- SIMULATED SMS LISTENING ---
                // In a real implementation, you would:
                // 1. Connect to the SMS Gateway's API/Webhook.
                // 2. Poll for new messages or wait for webhook calls.
                // 3. Extract the message content (the code) and sender information.

                // Example: Simulate receiving an SMS every 30 seconds
                // This is just for demonstration. Replace with actual SMS gateway logic.
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                // Simulate receiving an SMS
                // In reality, this data would come from the SMS gateway
                string simulatedSmsContent = "SAFERR ABC123XYZ789"; // Example format
                string simulatedSenderNumber = "+1234567890";
                string simulatedIpAddress = "203.0.113.1"; // Example IP

                _logger.LogInformation("Simulated SMS received from {Sender}: {Content}", simulatedSenderNumber, simulatedSmsContent);

                // Extract code (basic example - adjust parsing logic as needed)
                // Assuming format is "SAFERR <CODE>" or just the code
                string? codeToVerify = null;
                if (simulatedSmsContent.StartsWith("SAFERR ", StringComparison.OrdinalIgnoreCase))
                {
                    codeToVerify = simulatedSmsContent.Substring(7).Trim(); // Get part after "SAFERR "
                }
                else
                {
                    codeToVerify = simulatedSmsContent.Trim(); // Assume whole message is the code
                }

                if (string.IsNullOrWhiteSpace(codeToVerify))
                {
                    _logger.LogWarning("Received SMS with no recognizable code from {Sender}. Content: {Content}", simulatedSenderNumber, simulatedSmsContent);
                    continue; // Skip processing
                }

                // --- PROCESS THE VERIFICATION ---
                // Use a scope to resolve scoped services like ISecurityCodeService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var securityCodeService = scope.ServiceProvider.GetRequiredService<ISecurityCodeService>();

                    // Perform verification
                    var verificationResult = await securityCodeService.VerifyCodeAsync(
                        codeToVerify,
                        simulatedSenderNumber, // Log the sender
                        simulatedIpAddress     // Log the IP (if available from gateway)
                    );

                    _logger.LogInformation(
                        "Processed verification for code '{Code}' from {Sender}. Result: {Result} - {Message}",
                        codeToVerify,
                        simulatedSenderNumber,
                        verificationResult.Result,
                        verificationResult.Message
                    );

                    // --- SEND RESPONSE SMS ---
                    // Here, you would use the SMS Gateway API to send the `verificationResult.Message`
                    // back to `simulatedSenderNumber`.
                    // Example (conceptual):
                    // await _smsGatewayClient.SendMessageAsync(simulatedSenderNumber, verificationResult.Message);
                    //
                    _logger.LogInformation("Would send SMS response to {Sender}: {Message}", simulatedSenderNumber, verificationResult.Message);
                }
                // --- END PROCESSING ---
            }
            catch (OperationCanceledException)
            {
                // This is expected when the service is stopping
                _logger.LogInformation("SMS Listener Service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the SMS Listener Service.");
                // Depending on error type, you might want to implement retry logic or alerting
                // For now, we'll just log and continue listening (or stop if critical)
                // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying on error
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SMS Listener Service is stopping.");
        await base.StopAsync(cancellationToken);
    }
}

