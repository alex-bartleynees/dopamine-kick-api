using SharedKernel.Results;

namespace Payments.Domain.Errors;

public static class PaymentsErrors
{
    public static Error MissingUserId =>
        Error.Validation("Payments.MissingUserId", "User ID not found in claims.");

    public static Error MissingEmail =>
        Error.Validation("Payments.MissingEmail", "An email claim is required to create a Stripe customer.");

    public static Error NoCustomer =>
        Error.NotFound("Payments.NoCustomer", "No Stripe customer is associated with this user.");

    public static Error StripeFailure(string detail) =>
        Error.Failure("Payments.StripeFailure", detail);
}
