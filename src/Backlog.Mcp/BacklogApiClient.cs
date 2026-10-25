using System.Net.Http.Json;

public class BacklogApiClient(HttpClient http)
{
    public async Task<string> GetItemsAsync(string? type, string? status, string? search)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");

        var url = "/items" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var response = await http.GetAsync(url);
        return await ReadAsync(response);
    }

    public async Task<string> AddItemAsync(object request)
    {
        var response = await http.PostAsJsonAsync("/items", request);
        return await ReadAsync(response);
    }

    public async Task<string> UpdateStatusAsync(int id, string status)
    {
        var response = await http.PatchAsJsonAsync($"/items/{id}/status", new { status });
        return await ReadAsync(response);
    }

    // API'nin cevabını Claude'un okuyabileceği bir metne çevirir
    private static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
            return string.IsNullOrWhiteSpace(content) ? "İşlem başarılı." : content;

        return $"Hata ({(int)response.StatusCode}): {content}";
    }
    public async Task<string> GetSuggestionsAsync(int minutes, string? mood, string? type, int limit)
    {
        var query = new List<string> { $"minutes={minutes}", $"limit={limit}" };
        if (!string.IsNullOrWhiteSpace(mood)) query.Add($"mood={Uri.EscapeDataString(mood)}");
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");

        var response = await http.GetAsync("/suggestions?" + string.Join("&", query));
        return await ReadAsync(response);
    }
    public async Task<string> GetStaleItemsAsync(int days, int limit)
    {
        var response = await http.GetAsync($"/items/stale?days={days}&limit={limit}");
        return await ReadAsync(response);
    }
}