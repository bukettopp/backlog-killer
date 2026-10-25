using Backlog.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backlog.Api.Data;

public static class DevelopmentSeeder
{
    // Tablo boşsa test için örnek öğeler ekler. Sadece Development ortamında çağrılır.
    public static async Task SeedAsync(BacklogDbContext db, CancellationToken ct = default)
    {
        if (await db.BacklogItems.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;

        db.BacklogItems.AddRange(
            new BacklogItem
            {
                Title = "Hades",
                Type = ItemType.Game,
                EstimatedMinutes = 1320,
                Genres = ["Roguelike", "Action"],
                MoodTags = ["adventurous", "focused"],
                Status = ItemStatus.InProgress,
                Priority = 4,
                Source = "steam",
                ExternalId = "1145360",
                AddedAt = now.AddMonths(-8),
                LastTouchedAt = now.AddDays(-3)
            },
            new BacklogItem
            {
                Title = "Stardew Valley",
                Type = ItemType.Game,
                EstimatedMinutes = 3000,
                Genres = ["Simulation", "Farming"],
                MoodTags = ["chill"],
                Priority = 2,
                Source = "steam",
                ExternalId = "413150",
                AddedAt = now.AddMonths(-14)
            },
            new BacklogItem
            {
                Title = "Disco Elysium",
                Type = ItemType.Game,
                EstimatedMinutes = 1800,
                Genres = ["RPG", "Detective"],
                MoodTags = ["focused", "emotional"],
                Priority = 5,
                Source = "steam",
                ExternalId = "632470",
                AddedAt = now.AddMonths(-6)
            },
            new BacklogItem
            {
                Title = "Outer Wilds",
                Type = ItemType.Game,
                EstimatedMinutes = 1100,
                Genres = ["Exploration", "Puzzle"],
                MoodTags = ["adventurous", "emotional"],
                Status = ItemStatus.Done,
                Priority = 4,
                Source = "steam",
                ExternalId = "753640",
                AddedAt = now.AddMonths(-10),
                LastTouchedAt = now.AddMonths(-2),
                CompletedAt = now.AddMonths(-2)
            },
            new BacklogItem
            {
                Title = "Dune: Part Two",
                Type = ItemType.Movie,
                EstimatedMinutes = 166,
                Genres = ["Sci-Fi", "Adventure"],
                MoodTags = ["adventurous"],
                AddedAt = now.AddMonths(-1)
            },
            new BacklogItem
            {
                Title = "Spirited Away",
                Type = ItemType.Movie,
                EstimatedMinutes = 125,
                Genres = ["Animation", "Fantasy"],
                MoodTags = ["chill", "emotional"],
                Status = ItemStatus.Done,
                AddedAt = now.AddMonths(-5),
                LastTouchedAt = now.AddMonths(-4),
                CompletedAt = now.AddMonths(-4)
            },
            new BacklogItem
            {
                Title = "Arrival",
                Type = ItemType.Movie,
                EstimatedMinutes = 116,
                Genres = ["Sci-Fi", "Drama"],
                MoodTags = ["focused", "emotional"],
                Priority = 2,
                AddedAt = now.AddDays(-20)
            },
            new BacklogItem
            {
                Title = "Project Hail Mary",
                Type = ItemType.Book,
                EstimatedMinutes = 960,
                Genres = ["Sci-Fi"],
                MoodTags = ["adventurous", "focused"],
                Status = ItemStatus.InProgress,
                Priority = 5,
                AddedAt = now.AddMonths(-2),
                LastTouchedAt = now.AddDays(-1)
            },
            new BacklogItem
            {
                Title = "The Hobbit",
                Type = ItemType.Book,
                EstimatedMinutes = 600,
                Genres = ["Fantasy", "Adventure"],
                MoodTags = ["chill", "adventurous"],
                Status = ItemStatus.Dropped,
                Priority = 1,
                AddedAt = now.AddMonths(-12),
                LastTouchedAt = now.AddMonths(-9)
            },
            new BacklogItem
            {
                Title = "Klara and the Sun",
                Type = ItemType.Book,
                EstimatedMinutes = null, // süresi bilinmeyen örnek
                Genres = ["Literary Fiction"],
                MoodTags = ["emotional", "chill"],
                AddedAt = now.AddDays(-4)
            });

        await db.SaveChangesAsync(ct);
    }
}
