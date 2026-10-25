using Backlog.Api.Contracts;
using Backlog.Api.Entities;
using Backlog.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Backlog.Api.Endpoints;

public static class SuggestionEndpoints
{
    public static IEndpointRouteBuilder MapSuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/suggestions", GetSuggestions).WithTags("Suggestions");

        return app;
    }

    // GET /suggestions?minutes=120&mood=chill&type=Movie&limit=5
    private static async Task<Results<Ok<List<SuggestionResponse>>, ValidationProblem>> GetSuggestions(
        SuggestionService suggestionService,
        int? minutes,
        string? mood,
        ItemType? type,
        int? limit,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (minutes is null or <= 0)
            errors["minutes"] = ["Minutes zorunludur ve 0'dan büyük olmalıdır."];

        var normalizedMood = string.IsNullOrWhiteSpace(mood) ? null : mood.Trim().ToLowerInvariant();
        if (normalizedMood is not null && !SuggestionService.AllowedMoods.Contains(normalizedMood))
            errors["mood"] = [$"Mood şunlardan biri olmalıdır: {string.Join(", ", SuggestionService.AllowedMoods)}."];

        if (limit is <= 0)
            errors["limit"] = ["Limit 0'dan büyük olmalıdır."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        // Limit verilmezse varsayılan 5; 20'den büyükse 20'ye çekilir.
        var effectiveLimit = Math.Min(limit ?? SuggestionService.DefaultLimit, SuggestionService.MaxLimit);

        var suggestions = await suggestionService.SuggestAsync(
            minutes!.Value, normalizedMood, type, effectiveLimit, ct);

        return TypedResults.Ok(suggestions);
    }
}
