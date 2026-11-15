using System.Text.RegularExpressions;
using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Records;

public sealed partial record CreateRecordRequest(
    string? RecordNumber,
    string? Title,
    string? Description,
    int? AssignedToUserId)
{
    public void Validate()
    {
        new RequestValidator()
            .Required(nameof(RecordNumber), RecordNumber, 40, 3)
            .Pattern(nameof(RecordNumber), RecordNumber, RecordNumberRegex(),
                "Use letras, números, guion, guion bajo, punto o barra (sin espacios).")
            .Required(nameof(Title), Title, 200, 3)
            .Optional(nameof(Description), Description, 1000)
            .Positive(nameof(AssignedToUserId), AssignedToUserId, required: false)
            .ThrowIfInvalid();
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9\-_/\.]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RecordNumberRegex();
}

/// <summary>
/// Metadata/status update. Assignment changes go through the workflow (assign/transfer)
/// and closing goes through the close workflow so movement history stays coherent.
/// </summary>
public sealed record UpdateRecordRequest(
    string? Title,
    string? Description,
    short? RecordStatusId,
    string? RowVersion)
{
    public void Validate()
    {
        var v = new RequestValidator()
            .Required(nameof(Title), Title, 200, 3)
            .Optional(nameof(Description), Description, 1000)
            .Positive(nameof(RecordStatusId), RecordStatusId);

        if (!string.IsNullOrWhiteSpace(RowVersion) && !IsBase64(RowVersion))
            v.Add(nameof(RowVersion), "La versión de fila no es válida.");

        v.ThrowIfInvalid();
    }

    private static bool IsBase64(string value)
    {
        Span<byte> buffer = stackalloc byte[64];
        return Convert.TryFromBase64String(value, buffer, out var written) && written == 8;
    }
}

public sealed record RecordQuery(
    string? Search,
    short? StatusId,
    int? AssignedToUserId,
    bool? Unassigned,
    int? Page,
    int? PageSize);

public sealed record RecordSummary(
    long RecordId,
    string RecordNumber,
    string Title,
    string StatusCode,
    string Status,
    bool IsFinal,
    int? AssignedToUserId,
    string? AssignedToName,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc);

public sealed record RecordDetails(
    long RecordId,
    string RecordNumber,
    string Title,
    string? Description,
    short RecordStatusId,
    string StatusCode,
    string Status,
    bool IsFinal,
    int CreatedByUserId,
    string CreatedByName,
    int? AssignedToUserId,
    string? AssignedToName,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string RowVersion);

public sealed record RecordStatusItem(short RecordStatusId, string Code, string Name, bool IsFinal);
