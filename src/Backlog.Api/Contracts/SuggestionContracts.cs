using Backlog.Api.Entities;

namespace Backlog.Api.Contracts;

public record SuggestionResponse(
    int Id,
    string Title,
    ItemType Type,
    ItemStatus Status,
    int? EstimatedMinutes,
    List<string> MoodTags,
    int Score,
    List<string> Reasons);
