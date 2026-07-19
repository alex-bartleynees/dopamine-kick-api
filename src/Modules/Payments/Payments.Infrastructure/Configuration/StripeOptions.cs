namespace Payments.Infrastructure.Configuration;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>The recurring Price the Checkout Session subscribes the customer to.</summary>
    public string PriceId { get; set; } = string.Empty;

    /// <summary>Trial length applied per Checkout Session (0 = no trial).</summary>
    public int TrialPeriodDays { get; set; }

    public string SuccessUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>Where the Billing Portal returns the user after they finish managing their subscription.</summary>
    public string PortalReturnUrl { get; set; } = string.Empty;
}
