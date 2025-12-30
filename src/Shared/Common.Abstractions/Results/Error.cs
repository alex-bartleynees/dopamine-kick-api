namespace Common.Abstractions.Results;

public sealed record Error(int? Status, string Title, string Detail)
{
    public static readonly Error None = new(null, string.Empty, string.Empty);
}