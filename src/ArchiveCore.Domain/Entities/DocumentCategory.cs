namespace ArchiveCore.Domain.Entities;

public sealed class DocumentCategory
{
    public int DocumentCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Document> Documents { get; set; } = [];
}
