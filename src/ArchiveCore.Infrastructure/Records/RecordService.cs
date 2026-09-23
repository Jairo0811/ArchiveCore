using ArchiveCore.Application.Records;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Records;

public sealed class RecordService(ArchiveCoreDbContext db) : IRecordService
{
    public async Task<IReadOnlyCollection<RecordSummary>> ListAsync(
        string? search,
        short? statusId,
        int? assignedToUserId,
        CancellationToken cancellationToken)
    {
        var query = db.Records
            .AsNoTracking()
            .Include(x => x.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.RecordNumber.Contains(search) ||
                x.Title.Contains(search));

        if (statusId.HasValue)
            query = query.Where(x => x.RecordStatusId == statusId.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(x => x.AssignedToUserId == assignedToUserId.Value);

        return await query
            .OrderByDescending(x => x.OpenedAtUtc)
            .Select(x => new RecordSummary(
                x.RecordId,
                x.RecordNumber,
                x.Title,
                x.Status.Name,
                x.AssignedToUserId,
                x.OpenedAtUtc,
                x.ClosedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<RecordDetails?> GetAsync(
        long recordId,
        CancellationToken cancellationToken) =>
        await db.Records.AsNoTracking()
            .Where(x => x.RecordId == recordId)
            .Select(x => new RecordDetails(
                x.RecordId,
                x.RecordNumber,
                x.Title,
                x.Description,
                x.RecordStatusId,
                x.Status.Name,
                x.CreatedByUserId,
                x.AssignedToUserId,
                x.OpenedAtUtc,
                x.ClosedAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RecordDetails> CreateAsync(
        CreateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (await db.Records.AnyAsync(x => x.RecordNumber == request.RecordNumber, cancellationToken))
            throw new InvalidOperationException("Record number already exists.");

        var status = await db.RecordStatuses
            .SingleAsync(x => x.Code == "OPEN", cancellationToken);

        var entity = new ArchiveCore.Domain.Entities.Record
        {
            RecordNumber = request.RecordNumber.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            RecordStatusId = status.RecordStatusId,
            CreatedByUserId = actorUserId,
            AssignedToUserId = request.AssignedToUserId,
            OpenedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Records.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(entity.RecordId, cancellationToken))!;
    }

    public async Task<RecordDetails?> UpdateAsync(
        long recordId,
        UpdateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await db.Records.SingleOrDefaultAsync(
            x => x.RecordId == recordId,
            cancellationToken);

        if (entity is null)
            return null;

        var status = await db.RecordStatuses.SingleAsync(
            x => x.RecordStatusId == request.RecordStatusId,
            cancellationToken);

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.RecordStatusId = request.RecordStatusId;
        entity.AssignedToUserId = request.AssignedToUserId;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (status.IsFinal && entity.ClosedAtUtc is null)
            entity.ClosedAtUtc = DateTime.UtcNow;
        else if (!status.IsFinal)
            entity.ClosedAtUtc = null;

        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(recordId, cancellationToken);
    }
}
