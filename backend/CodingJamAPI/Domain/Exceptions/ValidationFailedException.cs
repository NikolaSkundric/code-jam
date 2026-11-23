namespace CodingJamAPI.Domain.Exceptions;

/// <summary>Business validation failure mapped to HTTP 400 with per-field errors.</summary>
public sealed class ValidationFailedException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationFailedException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }

    public ValidationFailedException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
