using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Records;

public interface IRecordService
{
    Task<PagedResult<RecordSummary>> ListAsync(RecordQuery query, CancellationToken cancellationToken);

    Task<RecordDetails> GetAsync(long recordId, CancellationToken cancellationToken);

    Task<RecordDetails> CreateAsync(
        CreateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<RecordDetails> UpdateAsync(
        long recordId,
        UpdateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RecordStatusItem>> ListStatusesAsync(CancellationToken cancellationToken);
}
