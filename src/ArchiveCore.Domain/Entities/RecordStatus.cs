namespace ArchiveCore.Domain.Entities;

public sealed class RecordStatus
{
    public short RecordStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFinal { get; set; }
    public short SortOrder { get; set; }

    public ICollection<Record> Records { get; set; } = [];
}
