using ArchiveCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Persistence;

public sealed class ArchiveCoreDbContext(DbContextOptions<ArchiveCoreDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Record> Records => Set<Record>();
    public DbSet<RecordStatus> RecordStatuses => Set<RecordStatus>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<MovementType> MovementTypes => Set<MovementType>();
    public DbSet<RecordMovement> RecordMovements => Set<RecordMovement>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArchiveCoreDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
