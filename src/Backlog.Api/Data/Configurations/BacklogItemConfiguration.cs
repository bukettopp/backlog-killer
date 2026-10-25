using Backlog.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backlog.Api.Data.Configurations;

public class BacklogItemConfiguration : IEntityTypeConfiguration<BacklogItem>
{
    public void Configure(EntityTypeBuilder<BacklogItem> builder)
    {
        // Sütun adı snake_case convention ile "priority" olur, constraint SQL'i buna göre yazılır.
        builder.ToTable(t => t.HasCheckConstraint("ck_backlog_items_priority", "priority BETWEEN 1 AND 5"));

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Enum'lar veritabanında string olarak saklanır.
        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ItemStatus.Backlog);

        builder.Property(x => x.Genres)
            .HasColumnType("text[]");

        builder.Property(x => x.MoodTags)
            .HasColumnType("text[]");

        builder.Property(x => x.Priority)
            .HasDefaultValue(3);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ExternalId)
            .HasMaxLength(100);

        builder.Property(x => x.AddedAt)
            .HasDefaultValueSql("now()");
    }
}
