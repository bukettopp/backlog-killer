using Backlog.Api.Entities;

namespace Backlog.Api.Contracts;

// Backlog'da bekleyen: hiç başlanmamış. InProgress'te bekleyen: yarıda bırakılmış.
public enum StaleCategory
{
    NeverStarted,
    Abandoned
}

public record StaleItemResponse(
    int Id,
    string Title,
    ItemType Type,
    ItemStatus Status,
    StaleCategory Category,
    int DaysWaiting,
    int Priority,
    int? EstimatedMinutes);
