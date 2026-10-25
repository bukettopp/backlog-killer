namespace Backlog.Api.Entities;

public class BacklogItem
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public ItemType Type { get; set; }

    // Tahmini bitirme süresi (dakika). Bilinmiyorsa null.
    public int? EstimatedMinutes { get; set; }

    // PostgreSQL'de text[] olarak saklanır.
    public List<string> Genres { get; set; } = [];

    // Örn: chill, focused, emotional, adventurous
    public List<string> MoodTags { get; set; } = [];

    public ItemStatus Status { get; set; } = ItemStatus.Backlog;

    // 1 (düşük) - 5 (yüksek)
    public int Priority { get; set; } = 3;

    // Kaydın nereden geldiği: "manual", "steam" ...
    public string Source { get; set; } = "manual";

    // Kaynak sistemdeki kimlik (örn. Steam app id)
    public string? ExternalId { get; set; }

    // Tüm tarihler UTC tutulur.
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTouchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Durum değişikliği kuralları tek yerde: dokunma zamanı ve tamamlanma zamanı.
    public void ChangeStatus(ItemStatus newStatus, DateTime utcNow)
    {
        if (Status == newStatus)
            return;

        Status = newStatus;
        LastTouchedAt = utcNow;
        CompletedAt = newStatus == ItemStatus.Done ? utcNow : null;
    }
}
