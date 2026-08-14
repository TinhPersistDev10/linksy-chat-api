namespace linksy_backend_api.Infrastructure.Exceptions;

/// <summary>
/// Registration conflict: one or more fields already exist (email / username).
/// </summary>
public sealed class RegisterConflictException : Exception
{
    public IReadOnlyDictionary<string, string> FieldErrors { get; }

    public RegisterConflictException(IDictionary<string, string> fieldErrors)
        : base(string.Join(". ", fieldErrors.Values))
    {
        FieldErrors = new Dictionary<string, string>(fieldErrors, StringComparer.OrdinalIgnoreCase);
    }
}
