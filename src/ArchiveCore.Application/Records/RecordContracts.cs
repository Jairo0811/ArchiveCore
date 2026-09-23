namespace ArchiveCore.Application.Records;

public sealed record CreateRecordRequest(
    string RecordNumber,
    string Title,
    string? Description,
    int? AssignedToUserId);

public sealed record UpdateRecordRequest(
    string Title,
    string? Description,
    short RecordStatusId,
    int? AssignedToUserId);

public sealed record RecordSummary(
    long RecordId,
    string RecordNumber,
    string Title,
    string Status,
    int? AssignedToUserId,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc);

public sealed record RecordDetails(
    long RecordId,
    string RecordNumber,
    string Title,
    string? Description,
    short RecordStatusId,
    string Status,
    int CreatedByUserId,
    int? AssignedToUserId,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
