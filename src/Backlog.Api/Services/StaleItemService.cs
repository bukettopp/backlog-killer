using Backlog.Api.Contracts;
using Backlog.Api.Data;
using Backlog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backlog.Api.Services;

public class StaleItemService(BacklogDbContext db)
{
    public const int DefaultDays = 90;
    public const int DefaultLimit = 10;
    public const int MaxLimit = 50;

    public async Task<List<StaleItemResponse>> GetStaleAsync(int days, int limit, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Referans tarihi bu eşikten eski olanlar en az "days" gündür bekliyordur.
        var threshold = now.AddDays(-days);

        // 1) Filtre, sıralama ve limit veritabanında uygulanır (COALESCE(last_touched_at, added_at)).
        var items = await db.BacklogItems.AsNoTracking()
            .Where(x => x.Status == ItemStatus.Backlog || x.Status == ItemStatus.InProgress)
            .Where(x => (x.LastTouchedAt ?? x.AddedAt) <= threshold)
            .OrderBy(x => x.LastTouchedAt ?? x.AddedAt)
            .ThenBy(x => x.Id)
            .Take(limit)
            .ToListAsync(ct);

        // 2) Gün sayısı ve kategori bellekte hesaplanır.
        return items.Select(item => ToResponse(item, now)).ToList();
    }

    private static StaleItemResponse ToResponse(BacklogItem item, DateTime utcNow)
    {
        var daysWaiting = (int)(utcNow - (item.LastTouchedAt ?? item.AddedAt)).TotalDays;

        var category = item.Status == ItemStatus.InProgress
            ? StaleCategory.Abandoned
            : StaleCategory.NeverStarted;

        return new StaleItemResponse(
            item.Id,
            item.Title,
            item.Type,
            item.Status,
            category,
            daysWaiting,
            item.Priority,
            item.EstimatedMinutes);
    }
}
