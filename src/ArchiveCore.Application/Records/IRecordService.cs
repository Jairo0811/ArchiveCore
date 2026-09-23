namespace ArchiveCore.Application.Records;

public interface IRecordService
{
    Task<IReadOnlyCollection<RecordSummary>> ListAsync(
        string? search,
        short? statusId,
        int? assignedToUserId,
        CancellationToken cancellationToken);

    Task<RecordDetails?> GetAsync(long recordId, CancellationToken cancellationToken);

    Task<RecordDetails> CreateAsync(
        CreateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<RecordDetails?> UpdateAsync(
        long recordId,
        UpdateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);
}
