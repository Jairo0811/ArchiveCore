namespace ArchiveCore.Application.Academic;

public sealed record SqlLabQueryDefinition(
    string Key,
    string Title,
    string LegacySql,
    string ModernSql,
    string Description,
    string SourcePath);

public sealed record SqlLabQueryResult(
    SqlLabQueryDefinition Query,
    IReadOnlyCollection<string> Columns,
    IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows);

public interface ISqlLabService
{
    IReadOnlyCollection<SqlLabQueryDefinition> GetQueries();

    Task<SqlLabQueryResult?> ExecuteAsync(
        string key,
        CancellationToken cancellationToken);
}
