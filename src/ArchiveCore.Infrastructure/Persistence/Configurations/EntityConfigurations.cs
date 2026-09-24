using ArchiveCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchiveCore.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", tb => tb.UseSqlOutputClause(false));
        b.HasKey(x => x.UserId);
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.NationalId).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(25).IsUnicode(false);
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.RoleId);
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles");
        b.HasKey(x => new { x.UserId, x.RoleId });
        b.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
        b.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
    }
}

public sealed class RecordStatusConfiguration : IEntityTypeConfiguration<RecordStatus>
{
    public void Configure(EntityTypeBuilder<RecordStatus> b)
    {
        b.ToTable("RecordStatuses");
        b.HasKey(x => x.RecordStatusId);
        b.Property(x => x.Code).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250);
    }
}

public sealed class RecordConfiguration : IEntityTypeConfiguration<Record>
{
    public void Configure(EntityTypeBuilder<Record> b)
    {
        b.ToTable("Records", tb => tb.UseSqlOutputClause(false));
        b.HasKey(x => x.RecordId);
        b.Property(x => x.RecordNumber).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Status).WithMany(x => x.Records).HasForeignKey(x => x.RecordStatusId);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> b)
    {
        b.ToTable("DocumentCategories");
        b.HasKey(x => x.DocumentCategoryId);
        b.Property(x => x.Code).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
    }
}

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("Documents", tb => tb.UseSqlOutputClause(false));
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.DocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Record).WithMany(x => x.Documents).HasForeignKey(x => x.RecordId);
        b.HasOne(x => x.Category).WithMany(x => x.Documents).HasForeignKey(x => x.DocumentCategoryId);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> b)
    {
        b.ToTable("DocumentVersions", tb => tb.UseSqlOutputClause(false));
        b.HasKey(x => x.DocumentVersionId);
        b.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        b.Property(x => x.FileExtension).HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.MimeType).HasMaxLength(150);
        b.Property(x => x.Sha256Hash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();

        b.HasOne(x => x.Document).WithMany(x => x.Versions).HasForeignKey(x => x.DocumentId);
        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class MovementTypeConfiguration : IEntityTypeConfiguration<MovementType>
{
    public void Configure(EntityTypeBuilder<MovementType> b)
    {
        b.ToTable("MovementTypes");
        b.HasKey(x => x.MovementTypeId);
        b.Property(x => x.Code).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250);
    }
}

public sealed class RecordMovementConfiguration : IEntityTypeConfiguration<RecordMovement>
{
    public void Configure(EntityTypeBuilder<RecordMovement> b)
    {
        b.ToTable("RecordMovements");
        b.HasKey(x => x.RecordMovementId);
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasOne(x => x.Record).WithMany(x => x.Movements).HasForeignKey(x => x.RecordId);
        b.HasOne(x => x.MovementType).WithMany(x => x.RecordMovements).HasForeignKey(x => x.MovementTypeId);
        b.HasOne(x => x.FromUser).WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.ToUser).WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("AuditEvents");
        b.HasKey(x => x.AuditEventId);
        b.Property(x => x.EntityName).HasMaxLength(128).IsRequired();
        b.Property(x => x.EntityKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.ActionType).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
