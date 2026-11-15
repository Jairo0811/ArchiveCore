using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ArchiveCore.Application.Common;

/// <summary>
/// Small, dependency-free validation helper used by request DTO validators.
/// Collects every error before throwing so clients receive a complete ProblemDetails payload.
/// </summary>
public sealed partial class RequestValidator
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValid => _errors.Count == 0;

    public RequestValidator Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(message);
        return this;
    }

    public RequestValidator Required(string field, string? value, int maxLength, int minLength = 1)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Add(field, "Este campo es obligatorio.");

        var length = value.Trim().Length;
        if (length < minLength)
            Add(field, $"Debe tener al menos {minLength} caracteres.");
        if (length > maxLength)
            Add(field, $"No puede superar {maxLength} caracteres.");

        return this;
    }

    public RequestValidator Optional(string field, string? value, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
            Add(field, $"No puede superar {maxLength} caracteres.");

        return this;
    }

    public RequestValidator Email(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Add(field, "El correo es obligatorio.");

        var trimmed = value.Trim();
        if (trimmed.Length > 256 || !IsValidEmail(trimmed))
            Add(field, "El correo no tiene un formato válido.");

        return this;
    }

    public RequestValidator Positive(string field, long? value, bool required = true)
    {
        if (value is null)
        {
            if (required)
                Add(field, "Este campo es obligatorio.");
            return this;
        }

        if (value <= 0)
            Add(field, "Debe ser un identificador positivo.");

        return this;
    }

    public RequestValidator Pattern(string field, string? value, Regex pattern, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && !pattern.IsMatch(value.Trim()))
            Add(field, message);

        return this;
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new RequestValidationException(
                _errors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
    }

    public static bool IsValidEmail(string value)
    {
        if (!SimpleEmailRegex().IsMatch(value))
            return false;

        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SimpleEmailRegex();
}
