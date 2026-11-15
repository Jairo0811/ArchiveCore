using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Workflow;

public sealed record TransferRecordRequest(int? ToUserId, string? Notes)
{
    public void Validate() =>
        new RequestValidator()
            .Positive(nameof(ToUserId), ToUserId)
            .Optional(nameof(Notes), Notes, 1000)
            .ThrowIfInvalid();
}

public sealed record CloseRecordRequest(string? Notes)
{
    public void Validate() =>
        new RequestValidator()
            .Optional(nameof(Notes), Notes, 1000)
            .ThrowIfInvalid();
}

public sealed record MovementSummary(
    long RecordMovementId,
    long RecordId,
    string MovementTypeCode,
    string MovementType,
    int? FromUserId,
    string? FromUserName,
    int? ToUserId,
    string? ToUserName,
    int PerformedByUserId,
    string PerformedByName,
    string? Notes,
    DateTime PerformedAtUtc);

public interface IWorkflowService
{
    Task<IReadOnlyCollection<MovementSummary>> GetHistoryAsync(
        long recordId,
        CancellationToken cancellationToken);

    /// <summary>Assigns (when unassigned) or transfers a record. Executed by dbo.sp_TransferRecord.</summary>
    Task TransferAsync(
        long recordId,
        TransferRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Closes a record. Executed by dbo.sp_CloseRecord.</summary>
    Task CloseAsync(
        long recordId,
        CloseRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);
}
