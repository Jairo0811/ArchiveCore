namespace ArchiveCore.Application.Auth;

/// <summary>
/// Single password policy shared by the first-administrator bootstrap and user provisioning.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static string Description =>
        $"Entre {MinLength} y {MaxLength} caracteres, con mayúscula, minúscula, número y símbolo.";

    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("La contraseña es obligatoria.");
            return errors;
        }

        if (password.Length < MinLength)
            errors.Add($"La contraseña debe tener al menos {MinLength} caracteres.");
        if (password.Length > MaxLength)
            errors.Add($"La contraseña no puede superar {MaxLength} caracteres.");
        if (!password.Any(char.IsUpper))
            errors.Add("La contraseña debe incluir una letra mayúscula.");
        if (!password.Any(char.IsLower))
            errors.Add("La contraseña debe incluir una letra minúscula.");
        if (!password.Any(char.IsDigit))
            errors.Add("La contraseña debe incluir un número.");
        if (password.All(char.IsLetterOrDigit))
            errors.Add("La contraseña debe incluir un símbolo.");
        if (password.Any(char.IsWhiteSpace) && password.Trim().Length != password.Length)
            errors.Add("La contraseña no puede empezar ni terminar con espacios.");

        return errors;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;
}
