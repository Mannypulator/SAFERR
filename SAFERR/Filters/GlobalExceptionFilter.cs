using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace SAFERR.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger; // Use ILogger for DI, but we'll primarily use Serilog's static Log

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        // Log the exception with full details using Serilog
        // Serilog's Log.ForContext provides context-specific logging if needed
        Log.ForContext("User", context.HttpContext.User?.Identity?.Name ?? "Anonymous")
           .ForContext("RequestId", context.HttpContext.TraceIdentifier)
           .ForContext("Action", context.ActionDescriptor.DisplayName)
           .Error(context.Exception, "An unhandled exception occurred while processing the request.");

        // Determine the response based on the exception type (optional)
        // For security, don't expose internal exception details directly to clients.
        var response = new ErrorResponse
        {
            // Assign a generic error code for internal tracking/metrics
            ErrorCode = "INTERNAL_ERROR",
            Message = "An internal server error occurred. Please try again later or contact support if the problem persists."
            // Details = context.Exception.ToString() // NEVER expose this in production!
        };

        // Set the result to return a 500 Internal Server Error with our custom response
        context.Result = new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            // Ensure the content type is set correctly for JSON
            ContentTypes = { "application/json" }
        };

        // Mark the exception as handled so it doesn't propagate further
        context.ExceptionHandled = true;
    }
}

// DTO for the standardized error response
public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    // public string Details { get; set; } = string.Empty; // DO NOT include in production responses
}

