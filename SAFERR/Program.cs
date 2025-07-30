using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SAFERR.BackgroundServices;
using SAFERR.Controllers;
using SAFERR.Data;
using SAFERR.Entities;
using SAFERR.Filters;
using SAFERR.Repositories;
using SAFERR.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    // .WriteTo.File("Logs/saferr-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30) // Keep logs for 30 days
    //                                                                                                     // Example for Seq (optional):
    //                                                                                                     // .WriteTo.Seq("http://localhost:5341") // If using Seq for log aggregation
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Reduce noise from framework
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information) // Show app start/stop
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Reduce noise from ASP.NET Core
    .Enrich.FromLogContext() // Include contextual information
    .Enrich.WithProperty("Application", "SaferrApp") // Add static property
    .CreateLogger();

builder.Host.UseSerilog(logger);

// builder.Services.Configure<TwilioSettings>(
//     builder.Configuration.GetSection("Twilio"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://localhost:3000", "https://yourdomain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers(options =>
{
    // --- Configure Global Error Handling Filter ---
    // This adds our custom exception filter globally to all controllers/actions
    options.Filters.Add<GlobalExceptionFilter>();
    // --------------------------------------------
}).AddJsonOptions(options =>
{
    // Handle circular references by ignoring cycles
    // This prevents the serializer from re-serializing already seen objects within the same object graph
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        
    // Optional: Configure other JSON serialization options
    // options.JsonSerializerOptions.WriteIndented = true; // For pretty-printing in development
    // options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SAFERR API", Version = "v1" });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING")));

// Register Repositories
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISecurityCodeRepository, SecurityCodeRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IBrandSubscriptionRepository, BrandSubscriptionRepository>();
builder.Services.AddScoped<IVerificationLogRepository, VerificationLogRepository>();
builder.Services.AddScoped<ISecurityCodeService, SecurityCodeService>();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
// var jwtSettings = builder.Configuration.GetSection("Jwt");
// builder.Services.Configure<JwtSettings>(jwtSettings);
var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Reduce or eliminate clock skew if necessary
    };
});
builder.Services.AddAuthorization();
// builder.Services.AddHostedService<SmsListenerService>();

// --- Configure Stripe Settings (if BillingService is implemented) ---
// builder.Services.Configure<StripeSettings>(
//     builder.Configuration.GetSection("Stripe"));

// Register Billing Service (if implemented)
// builder.Services.AddScoped<IBillingService, BillingService>();
// --


var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "Handled {RequestPath} in {Elapsed:0.0000} ms";

    // Customize the logging level (default is Information)
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error // Log errors as Error
        : elapsed > 1000
            ? LogEventLevel.Warning // Log slow requests as Warning
            : LogEventLevel.Information; // Log normal requests as Info

    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        // Be cautious logging sensitive data like full headers or body
    };
});


// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors("AllowAll");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Log.Information("Ensuring database is created/migrated...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database ensured.");

        // --- Seed Default Free Subscription Plan ---
        Log.Information("Checking for default free subscription plan...");
        var freePlanExists = await dbContext.SubscriptionPlans.AnyAsync(sp => sp.Name == "Free");
        if (!freePlanExists)
        {
            Log.Information("Creating default free subscription plan...");
            var freePlan = new SubscriptionPlan
            {
                Name = "Free",
                Description = "Basic plan for getting started. Limited to 100 codes per product.",
                Price = 0m, // Free
                MaxCodesPerMonth = 100, // As requested
                MaxVerificationsPerMonth = -1, // Unlimited verifications
                IsActive = true,
                // StripePriceId = null // Not needed for free plan, or set to a specific free price ID in Stripe if you create one
            };
            dbContext.SubscriptionPlans.Add(freePlan);
            await dbContext.SaveChangesAsync();
            Log.Information("Default free subscription plan created.");
        }
        else
        {
            Log.Information("Default free subscription plan already exists.");
        }
        // ------------------------------------------
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while creating/migrating the database or seeding data.");
}
finally
{
    await Log.CloseAndFlushAsync();
}



app.UseHttpsRedirection();

app.UseAuthentication(); // Add this
app.UseAuthorization();

app.MapControllers();

app.Run();

// public class TwilioSettings
// {
//     public string? AccountSid { get; set; }
//     public string? AuthToken { get; set; }
//     public string? PhoneNumber { get; set; } // The Twilio number that receives SMS
// }