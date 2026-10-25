using Backlog.Api.Contracts;
using Backlog.Api.Data;
using Backlog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backlog.Api.Services;

public class SuggestionService(BacklogDbContext db)
{
    public static readonly string[] AllowedMoods = ["chill", "focused", "emotional", "adventurous"];

    public const int DefaultLimit = 5;
    public const int MaxLimit = 20;

    public async Task<List<SuggestionResponse>> SuggestAsync(
        int minutes, string? mood, ItemType? type, int limit, CancellationToken ct)
    {
        // 1) Filtreler veritabanında uygulanır.
        var query = db.BacklogItems.AsNoTracking()
            .Where(x => x.Status == ItemStatus.Backlog || x.Status == ItemStatus.InProgress);

        if (type is not null)
            query = query.Where(x => x.Type == type);

        // Film yarıda bırakılmaz: süresi bilinmeyen veya süreye sığmayan filmler aday olamaz.
        // Oyun ve kitap parça parça tüketilebildiği için süreden bağımsız adaydır.
        query = query.Where(x => x.Type != ItemType.Movie
            || (x.EstimatedMinutes != null && x.EstimatedMinutes <= minutes));

        var candidates = await query.ToListAsync(ct);

        // 2) Puanlama bellekte yapılır.
        var now = DateTime.UtcNow;

        return candidates
            .Select(item => Score(item, minutes, mood, now))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Response.Id)
            .Take(limit)
            .Select(x => x.Response)
            .ToList();
    }

    private static (SuggestionResponse Response, int Score, int Priority) Score(
        BacklogItem item, int minutes, string? mood, DateTime utcNow)
    {
        var score = 0;
        var reasons = new List<string>();

        if (mood is not null && item.MoodTags.Contains(mood, StringComparer.OrdinalIgnoreCase))
        {
            score += 3;
            reasons.Add($"Ruh haline uyuyor ({mood})");
        }

        if (item.EstimatedMinutes is { } estimated && estimated <= minutes)
        {
            score += 2;
            reasons.Add($"Bu akşam bitirebilirsin ({estimated} dk)");
        }

        if (item.Status == ItemStatus.InProgress)
        {
            score += 2;
            reasons.Add("Devam ettiğin bir şey");
        }

        score += item.Priority;
        reasons.Add(item.Priority switch
        {
            >= 4 => $"Yüksek öncelikli ({item.Priority}/5)",
            3 => $"Orta öncelikli ({item.Priority}/5)",
            _ => $"Düşük öncelikli ({item.Priority}/5)"
        });

        // Her 30 gün bekleme için +1, en fazla +3.
        var waitingDays = (int)(utcNow - (item.LastTouchedAt ?? item.AddedAt)).TotalDays;
        var waitingPoints = Math.Min(waitingDays / 30, 3);
        if (waitingPoints > 0)
        {
            score += waitingPoints;
            reasons.Add($"{waitingDays} gündür bekliyor");
        }

        var response = new SuggestionResponse(
            item.Id,
            item.Title,
            item.Type,
            item.Status,
            item.EstimatedMinutes,
            item.MoodTags,
            score,
            reasons);

        return (response, score, item.Priority);
    }
}
