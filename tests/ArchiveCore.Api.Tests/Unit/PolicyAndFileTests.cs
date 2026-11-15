using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;
using ArchiveCore.Infrastructure.Documents;
using Xunit;

namespace ArchiveCore.Api.Tests.Unit;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("short1!A")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoDigitsHere!!x")]
    [InlineData("NoSymbols12345x")]
    [InlineData(" Leading1!space")]
    public void Weak_passwords_are_rejected(string password) =>
        Assert.False(PasswordPolicy.IsValid(password));

    [Fact]
    public void Strong_password_is_accepted() =>
        Assert.True(PasswordPolicy.IsValid(TestSecrets.NewPassword()));
}

public sealed class RequestValidatorTests
{
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("user.name+tag@sub.example.org", true)]
    [InlineData("not-an-email", false)]
    [InlineData("a@b", false)]
    [InlineData("Name <user@example.com>", false)]
    public void Email_format(string value, bool expected) =>
        Assert.Equal(expected, RequestValidator.IsValidEmail(value));

    [Fact]
    public void Paging_is_bounded()
    {
        var page = PageRequest.From(-3, 10_000);
        Assert.Equal(1, page.Page);
        Assert.Equal(PageRequest.MaxPageSize, page.PageSize);
    }
}

public sealed class FileTypeInspectorTests
{
    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("..\\..\\windows\\system.ini.txt", "system.ini.txt")]
    [InlineData("informe:final?.pdf", "informe_final_.pdf")]
    [InlineData("   ", "documento")]
    public void File_names_are_sanitized(string input, string expected) =>
        Assert.Equal(expected, FileTypeInspector.SanitizeFileName(input));

    [Fact]
    public void Pdf_signature_must_match_extension()
    {
        var pdf = FileTypeInspector.FindByExtension(".pdf")!;
        Assert.True(FileTypeInspector.MatchesSignature(pdf, "%PDF-1.7\n"u8));
        Assert.False(FileTypeInspector.MatchesSignature(pdf, "MZ\u0090\0"u8));
    }

    [Fact]
    public void Executables_are_not_allowed() =>
        Assert.Null(FileTypeInspector.FindByExtension(".exe"));

    [Fact]
    public void Binary_content_is_not_plain_text()
    {
        var txt = FileTypeInspector.FindByExtension(".txt")!;
        Assert.False(FileTypeInspector.MatchesSignature(txt, new byte[] { 0x41, 0x00, 0x42 }));
        Assert.True(FileTypeInspector.MatchesSignature(txt, "Hola, expediente ñ"u8));
    }
}
