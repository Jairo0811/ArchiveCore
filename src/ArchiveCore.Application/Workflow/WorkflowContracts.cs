namespace ArchiveCore.Application.Workflow;

public sealed record TransferRecordRequest(int ToUserId, string? Notes);
public sealed record CloseRecordRequest(string? Notes);

public sealed record MovementSummary(
    long RecordMovementId,
    long RecordId,
    string MovementType,
    int? FromUserId,
    int? ToUserId,
    int PerformedByUserId,
    string? Notes,
    DateTime PerformedAtUtc);

public interface IWorkflowService
{
    Task<IReadOnlyCollection<MovementSummary>> GetHistoryAsync(
        long recordId,
        CancellationToken cancellationToken);

    Task TransferAsync(
        long recordId,
        TransferRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    Task CloseAsync(
        long recordId,
        CloseRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);
}
