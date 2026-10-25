using Backlog.Api.Contracts;
using Backlog.Api.Data;
using Backlog.Api.Entities;
using Backlog.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Backlog.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/items").WithTags("Items");

        group.MapGet("/", GetItems);
        // {id:int} kısıtı sayesinde "/items/stale" GetItemById'a düşmez.
        group.MapGet("/stale", GetStaleItems);
        group.MapGet("/{id:int}", GetItemById).WithName(nameof(GetItemById));
        group.MapPost("/", CreateItem);
        group.MapPatch("/{id:int}/status", UpdateStatus);
        group.MapDelete("/{id:int}", DeleteItem);

        return app;
    }

    // GET /items?type=Game&status=Backlog&search=hades
    private static async Task<Ok<List<ItemResponse>>> GetItems(
        BacklogDbContext db,
        ItemType? type,
        ItemStatus? status,
        string? search,
        CancellationToken ct)
    {
        var query = db.BacklogItems.AsNoTracking();

        if (type is not null)
            query = query.Where(x => x.Type == type);

        if (status is not null)
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term));
        }

        var items = await query
            .OrderByDescending(x => x.AddedAt)
            .ToListAsync(ct);

        return TypedResults.Ok(items.Select(ItemResponse.FromEntity).ToList());
    }

    // GET /items/stale?days=90&limit=10
    private static async Task<Results<Ok<List<StaleItemResponse>>, ValidationProblem>> GetStaleItems(
        StaleItemService staleItemService,
        int? days,
        int? limit,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (days is < 1)
            errors["days"] = ["Days en az 1 olmalıdır."];

        if (limit is <= 0)
            errors["limit"] = ["Limit 0'dan büyük olmalıdır."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        // Limit verilmezse varsayılan 10; 50'den büyükse 50'ye çekilir.
        var effectiveLimit = Math.Min(limit ?? StaleItemService.DefaultLimit, StaleItemService.MaxLimit);

        var items = await staleItemService.GetStaleAsync(
            days ?? StaleItemService.DefaultDays, effectiveLimit, ct);

        return TypedResults.Ok(items);
    }

    // GET /items/{id}
    private static async Task<Results<Ok<ItemResponse>, NotFound>> GetItemById(
        int id, BacklogDbContext db, CancellationToken ct)
    {
        var item = await db.BacklogItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ItemResponse.FromEntity(item));
    }

    // POST /items
    private static async Task<Results<CreatedAtRoute<ItemResponse>, ValidationProblem>> CreateItem(
        CreateItemRequest request, BacklogDbContext db, CancellationToken ct)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var item = new BacklogItem
        {
            Title = request.Title!.Trim(),
            Type = request.Type!.Value,
            EstimatedMinutes = request.EstimatedMinutes,
            Genres = request.Genres ?? [],
            MoodTags = request.MoodTags ?? [],
            Priority = request.Priority ?? 3,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim(),
            ExternalId = request.ExternalId,
            // Bu alanları istemci değil backend belirler.
            Status = ItemStatus.Backlog,
            AddedAt = DateTime.UtcNow
        };

        db.BacklogItems.Add(item);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(
            ItemResponse.FromEntity(item), nameof(GetItemById), new { id = item.Id });
    }

    // PATCH /items/{id}/status
    private static async Task<Results<Ok<ItemResponse>, NotFound, ValidationProblem>> UpdateStatus(
        int id, UpdateStatusRequest request, BacklogDbContext db, CancellationToken ct)
    {
        if (request.Status is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Status zorunludur."]
            });
        }

        var item = await db.BacklogItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
            return TypedResults.NotFound();

        item.ChangeStatus(request.Status.Value, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ItemResponse.FromEntity(item));
    }

    // DELETE /items/{id}
    private static async Task<Results<NoContent, NotFound>> DeleteItem(
        int id, BacklogDbContext db, CancellationToken ct)
    {
        var deleted = await db.BacklogItems.Where(x => x.Id == id).ExecuteDeleteAsync(ct);

        return deleted == 0
            ? TypedResults.NotFound()
            : TypedResults.NoContent();
    }

    private static Dictionary<string, string[]> Validate(CreateItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title boş olamaz."];
        else if (request.Title.Trim().Length > 200)
            errors["title"] = ["Title en fazla 200 karakter olabilir."];

        if (request.Type is null)
            errors["type"] = ["Type zorunludur (Game, Movie, Book)."];

        if (request.Priority is < 1 or > 5)
            errors["priority"] = ["Priority 1 ile 5 arasında olmalıdır."];

        if (request.Source?.Trim().Length > 50)
            errors["source"] = ["Source en fazla 50 karakter olabilir."];

        if (request.ExternalId?.Length > 100)
            errors["externalId"] = ["ExternalId en fazla 100 karakter olabilir."];

        return errors;
    }
}
