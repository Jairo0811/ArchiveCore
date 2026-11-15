using System.Data;
using ArchiveCore.Application.Common;
using ArchiveCore.Application.Records;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Records;

public sealed class RecordService(ArchiveCoreDbContext db) : IRecordService
{
    public async Task<PagedResult<RecordSummary>> ListAsync(
        RecordQuery request,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.From(request.Page, request.PageSize);
        var query = db.Records.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (search.Length > 200)
                search = search[..200];

            query = query.Where(x =>
                x.RecordNumber.Contains(search) ||
                x.Title.Contains(search));
        }

        if (request.StatusId is > 0)
            query = query.Where(x => x.RecordStatusId == request.StatusId.Value);

        if (request.Unassigned == true)
            query = query.Where(x => x.AssignedToUserId == null);
        else if (request.AssignedToUserId is > 0)
            query = query.Where(x => x.AssignedToUserId == request.AssignedToUserId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OpenedAtUtc)
            .ThenByDescending(x => x.RecordId)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(x => new RecordSummary(
                x.RecordId,
                x.RecordNumber,
                x.Title,
                x.Status.Code,
                x.Status.Name,
                x.Status.IsFinal,
                x.AssignedToUserId,
                x.AssignedToUser == null ? null : x.AssignedToUser.FirstName + " " + x.AssignedToUser.LastName,
                x.OpenedAtUtc,
                x.ClosedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<RecordSummary>(items, page.Page, page.PageSize, total);
    }

    public async Task<RecordDetails> GetAsync(long recordId, CancellationToken cancellationToken)
    {
        var record = await db.Records.AsNoTracking()
            .Where(x => x.RecordId == recordId)
            .Select(x => new
            {
                x.RecordId,
                x.RecordNumber,
                x.Title,
                x.Description,
                x.RecordStatusId,
                StatusCode = x.Status.Code,
                Status = x.Status.Name,
                x.Status.IsFinal,
                x.CreatedByUserId,
                CreatedByName = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName,
                x.AssignedToUserId,
                AssignedToName = x.AssignedToUser == null
                    ? null
                    : x.AssignedToUser.FirstName + " " + x.AssignedToUser.LastName,
                x.OpenedAtUtc,
                x.ClosedAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("El expediente no existe.");

        return new RecordDetails(
            record.RecordId,
            record.RecordNumber,
            record.Title,
            record.Description,
            record.RecordStatusId,
            record.StatusCode,
            record.Status,
            record.IsFinal,
            record.CreatedByUserId,
            record.CreatedByName,
            record.AssignedToUserId,
            record.AssignedToName,
            record.OpenedAtUtc,
            record.ClosedAtUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            Convert.ToBase64String(record.RowVersion));
    }

    /// <summary>
    /// Creation goes through dbo.sp_CreateRecord so the initial assignment movement,
    /// user validation and audit actor follow exactly the same rules as the SQL layer.
    /// </summary>
    public async Task<RecordDetails> CreateAsync(
        CreateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var recordId = await StoredProcedures.ExecuteScalarAsync<long>(
            db,
            "dbo.sp_CreateRecord",
            [
                StoredProcedures.Param("@RecordNumber", SqlDbType.VarChar, request.RecordNumber!.Trim(), 40),
                StoredProcedures.Param("@Title", SqlDbType.NVarChar, request.Title!.Trim(), 200),
                StoredProcedures.Param("@Description", SqlDbType.NVarChar, NullIfEmpty(request.Description), 1000),
                StoredProcedures.Param("@CreatedByUserId", SqlDbType.Int, actorUserId),
                StoredProcedures.Param("@AssignedToUserId", SqlDbType.Int, request.AssignedToUserId)
            ],
            cancellationToken);

        return await GetAsync(recordId, cancellationToken);
    }

    public async Task<RecordDetails> UpdateAsync(
        long recordId,
        UpdateRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var entity = await db.Records
            .Include(x => x.Status)
            .SingleOrDefaultAsync(x => x.RecordId == recordId, cancellationToken)
            ?? throw new ResourceNotFoundException("El expediente no existe.");

        if (entity.Status.IsFinal)
            throw new ConflictException("El expediente está cerrado y no puede modificarse.");

        var status = await db.RecordStatuses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RecordStatusId == request.RecordStatusId, cancellationToken)
            ?? throw new RequestValidationException(nameof(request.RecordStatusId), "El estado no existe.");

        if (status.IsFinal)
            throw new RequestValidationException(
                nameof(request.RecordStatusId),
                "Para cerrar un expediente utiliza la acción de cierre.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            // Optimistic concurrency: the client sends the rowversion it last read.
            db.Entry(entity).Property(x => x.RowVersion).OriginalValue =
                Convert.FromBase64String(request.RowVersion);
        }

        entity.Title = request.Title!.Trim();
        entity.Description = NullIfEmpty(request.Description);
        entity.RecordStatusId = status.RecordStatusId;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await SqlErrorTranslator.RunAsync(() => db.SaveChangesAsync(cancellationToken));
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "El expediente fue modificado por otro usuario. Recarga los datos e inténtalo de nuevo.");
        }

        return await GetAsync(recordId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RecordStatusItem>> ListStatusesAsync(CancellationToken cancellationToken) =>
        await db.RecordStatuses.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new RecordStatusItem(x.RecordStatusId, x.Code, x.Name, x.IsFinal))
            .ToArrayAsync(cancellationToken);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
