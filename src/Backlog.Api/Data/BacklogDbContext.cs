using Backlog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backlog.Api.Data;

public class BacklogDbContext(DbContextOptions<BacklogDbContext> options) : DbContext(options)
{
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bu assembly'deki tüm IEntityTypeConfiguration sınıflarını uygular.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BacklogDbContext).Assembly);
    }
}
