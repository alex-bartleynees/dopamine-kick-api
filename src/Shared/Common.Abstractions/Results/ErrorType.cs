namespace Common.Abstractions.Results;

/// <summary>
/// Semantic classification of an <see cref="Error"/>. This is intentionally
/// transport-agnostic: the application layer describes <em>what kind</em> of
/// failure occurred, and the API layer decides how to represent it (HTTP status,
/// problem body, etc.). Never put raw HTTP status codes in the application layer.
/// </summary>
public enum ErrorType
{
    /// <summary>An unexpected/unhandled failure. Maps to 500.</summary>
    Failure,

    /// <summary>Invalid input or a broken domain rule. Maps to 400.</summary>
    Validation,

    /// <summary>A requested resource does not exist (or is not visible to the caller). Maps to 404.</summary>
    NotFound,

    /// <summary>The request conflicts with existing state. Maps to 409.</summary>
    Conflict,

    /// <summary>The caller is not authenticated/authorized. Maps to 401.</summary>
    Unauthorized,

    /// <summary>The resource is no longer available. Maps to 410.</summary>
    Gone,
}
