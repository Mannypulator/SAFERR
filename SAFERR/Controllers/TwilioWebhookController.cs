using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SAFERR.Services;
using Twilio.TwiML;
using Twilio.TwiML.Messaging;

namespace SAFERR.Controllers
{

    [ApiController]
    // The route should match what you configure in your Twilio console for the phone number's webhook
    [Route("api/[controller]/[action]")]
    public class TwilioWebhookController : ControllerBase // Inherit from TwilioController for helpers
    {
        private readonly ISecurityCodeService _securityCodeService;
        private readonly Serilog.ILogger _logger;
        // private readonly TwilioSettings _twilioSettings;

        public TwilioWebhookController(
            ISecurityCodeService securityCodeService,
            Serilog.ILogger logger) // Inject settings
        {
            _securityCodeService = securityCodeService;
            _logger = logger.ForContext<TwilioWebhookController>();
            // _twilioSettings = twilioOptions.Value;
        }

        /// <summary>
        /// Handles incoming SMS messages from Twilio.
        /// This endpoint URL must be configured in your Twilio console for your phone number's 'A Message Comes In' webhook.
        /// </summary>
        /// <param name="request">The data sent by Twilio in the webhook request.</param>
        /// <returns>A TwiML response containing the SMS reply.</returns>
        [HttpPost("HandleIncomingSms")]
        [Produces("application/xml")] // Twilio expects XML (TwiML) response
        public async Task<IActionResult> HandleIncomingSms([FromForm] IncomingSmsRequest request)
        {
            var requestLogger = _logger
              .ForContext("TwilioSmsSid", request.SmsSid ?? "N/A")
              .ForContext("From", request.From ?? "N/A")
              .ForContext("To", request.To ?? "N/A");

            requestLogger.Information(
           "Received Twilio webhook for incoming SMS. From: {From}, To: {To}, Body: '{Body}'",
           request.From, request.To, request.Body);

            string senderNumber = request.From ?? "Unknown";
            string recipientNumber = request.To ?? "Unknown";
            string smsContent = request.Body ?? "";

            var processingStopwatch = Stopwatch.StartNew();

            // --- PROCESS THE VERIFICATION ---
            string responseMessage;

            try
            {
                // 1. Extract Code
                // Assuming the SMS body *is* the code, or the code is the last word/part.
                // Adjust parsing logic based on expected user input format.
                // Example formats: "ABC123XYZ789", "VERIFY ABC123XYZ789", "SAFERR ABC123XYZ789"
                string? codeToVerify = smsContent.Trim(); // Simplest: whole body is code
                                                          // More robust parsing example:
                                                          // var parts = smsContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                                          // codeToVerify = parts.LastOrDefault()?.Trim(); // Get the last part

                if (string.IsNullOrWhiteSpace(codeToVerify))
                {
                    responseMessage = "Invalid request. Please send the product code.";
                    requestLogger.Warning("Received SMS with no recognizable code. Content: '{Content}'", smsContent);
                }
                else
                {
                    // 2. Perform Verification using existing service
                    // Note: Twilio webhook requests don't typically contain the IP of the sender's phone,
                    // but rather the IP of Twilio's servers. We can log the sender number instead.
                    // If IP is crucial, it would need to be captured differently or might not be available.
                    var verificationResult = await _securityCodeService.VerifyCodeAsync(
                        codeToVerify,
                        senderNumber,        // Log the sender's phone number
                        HttpContext?.Connection?.RemoteIpAddress?.ToString() // IP of Twilio server (less useful)
                    );

                    responseMessage = verificationResult.Message; // Use the message from the service

                    requestLogger.Information(
                        "Processed verification for code '{Code}' from {Sender}. Result: {Result}",
                        codeToVerify,
                        senderNumber,
                        verificationResult.Result
                    );

                    processingStopwatch.Stop();
                }
            }
            catch (Exception ex)
            {
                processingStopwatch.Stop();
                requestLogger.Error(ex, "An unexpected error occurred processing SMS from {Sender}: '{Content}'", senderNumber, smsContent);
                responseMessage = "Sorry, an error occurred processing your request. Please try again later.";
            }

            bool smsSent = await _securityCodeService.SendSmsAsync(senderNumber, responseMessage);

            if (smsSent)
            {
                requestLogger.Information("Successfully sent verification response SMS to {Sender}.", senderNumber);
            }
            else
            {
                requestLogger.Error("Failed to send verification response SMS to {Sender}.", senderNumber);
                // Depending on requirements, you might want to return a different status code
                // or implement retry logic here. For now, we'll still acknowledge the webhook.
            }

            // --- CREATE TWILIO RESPONSE (TwiML) ---
            var response = new MessagingResponse();
            var message = new Message(responseMessage);
            response.Append(message);

            // Return the TwiML response so Twilio knows what SMS to send back
            var twimlXml = response.ToString();

            // Return the XML content with the correct content type
            return Content(twimlXml, "application/xml");
        }
    }

    // DTO to strongly bind the data Twilio sends in the webhook POST request
    // Twilio sends data as application/x-www-form-urlencoded
    public class IncomingSmsRequest
    {
        // Essential fields from Twilio's webhook
        public string? SmsSid { get; set; }
        public string? SmsStatus { get; set; }
        public string? SmsMessageSid { get; set; }
        public string? AccountSid { get; set; }
        public string? MessagingServiceSid { get; set; }
        public string? From { get; set; } // Sender's phone number (e.g., +1234567890)
        public string? To { get; set; }   // Your Twilio phone number (e.g., +1234567890)
        public string? Body { get; set; } // The text content of the SMS
        public string? NumMedia { get; set; } // Number of media files
        public string? FromCity { get; set; }
        public string? FromState { get; set; }
        public string? FromZip { get; set; }
        public string? FromCountry { get; set; }
        public string? ToCity { get; set; }
        public string? ToState { get; set; }
        public string? ToZip { get; set; }
        public string? ToCountry { get; set; }
        // Add other fields as needed based on Twilio docs
    }
}

