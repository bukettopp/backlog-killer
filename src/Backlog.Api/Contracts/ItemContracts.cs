using Backlog.Api.Entities;

namespace Backlog.Api.Contracts;

public record CreateItemRequest(
    string? Title,
    ItemType? Type,
    int? EstimatedMinutes,
    List<string>? Genres,
    List<string>? MoodTags,
    int? Priority,
    string? Source,
    string? ExternalId);

public record UpdateStatusRequest(ItemStatus? Status);

public record ItemResponse(
    int Id,
    string Title,
    ItemType Type,
    int? EstimatedMinutes,
    List<string> Genres,
    List<string> MoodTags,
    ItemStatus Status,
    int Priority,
    string Source,
    string? ExternalId,
    DateTime AddedAt,
    DateTime? LastTouchedAt,
    DateTime? CompletedAt)
{
    public static ItemResponse FromEntity(BacklogItem item) => new(
        item.Id,
        item.Title,
        item.Type,
        item.EstimatedMinutes,
        item.Genres,
        item.MoodTags,
        item.Status,
        item.Priority,
        item.Source,
        item.ExternalId,
        item.AddedAt,
        item.LastTouchedAt,
        item.CompletedAt);
}
