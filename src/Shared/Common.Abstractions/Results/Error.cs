namespace Common.Abstractions.Results;

/// <summary>
/// A failure descriptor produced by the application/domain layers.
/// <para>
/// The layers that create errors only ever supply a machine-readable
/// <paramref name="Code"/>, a human-readable <paramref name="Detail"/> message, and a
/// semantic <paramref name="Type"/>. The HTTP <see cref="Status"/> and <see cref="Title"/>
/// are <em>derived</em> from <paramref name="Type"/> at the boundary — raw status codes must
/// never be hand-written in the application layer.
/// </para>
/// </summary>
public sealed record Error(string Code, string Detail, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>HTTP status code derived from <see cref="Type"/>. Presentation concern; serialized for clients.</summary>
    public int Status => Type switch
    {
        ErrorType.Validation => 400,
        ErrorType.Unauthorized => 401,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Gone => 410,
        ErrorType.Failure => 500,
        _ => 500,
    };

    /// <summary>Human-friendly title derived from <see cref="Type"/>. Presentation concern; serialized for clients.</summary>
    public string Title => Type switch
    {
        ErrorType.Validation => "Bad Request",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Gone => "Gone",
        ErrorType.Failure => "Server Error",
        _ => "Server Error",
    };

    public static Error Failure(string code, string detail) => new(code, detail, ErrorType.Failure);
    public static Error Validation(string code, string detail) => new(code, detail, ErrorType.Validation);
    public static Error NotFound(string code, string detail) => new(code, detail, ErrorType.NotFound);
    public static Error Conflict(string code, string detail) => new(code, detail, ErrorType.Conflict);
    public static Error Unauthorized(string code, string detail) => new(code, detail, ErrorType.Unauthorized);
    public static Error Gone(string code, string detail) => new(code, detail, ErrorType.Gone);
}
