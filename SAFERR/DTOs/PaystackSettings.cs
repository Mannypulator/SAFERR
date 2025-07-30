namespace SAFERR.DTOs;

public class PaystackSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}

public class StripeSettings
{
    public string SecretKey { get; set; }
    public string PublishableKey { get; set; }
    public string WebhookSecret { get; set; }
}