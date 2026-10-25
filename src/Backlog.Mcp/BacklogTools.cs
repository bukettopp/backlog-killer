using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class BacklogTools
{
    [McpServerTool(Name = "search_backlog")]
    [Description("Kullanıcının backlog'undaki (oynayacağı, izleyeceği, okuyacağı şeyler) öğeleri listeler ve arar. " +
                 "Bir öğenin id'sini bulmak için de bu aracı kullan. Filtrelerin hepsi isteğe bağlıdır.")]
    public static Task<string> SearchBacklog(
        BacklogApiClient api,
        [Description("Tür filtresi: Game, Movie veya Book")] string? type = null,
        [Description("Durum filtresi: Backlog, InProgress, Done veya Dropped")] string? status = null,
        [Description("Başlıkta aranacak metin")] string? search = null)
    {
        return api.GetItemsAsync(type, status, search);
    }

    [McpServerTool(Name = "add_item")]
    [Description("Backlog'a yeni bir oyun, film veya kitap ekler.")]
    public static Task<string> AddItem(
        BacklogApiClient api,
        [Description("Başlık")] string title,
        [Description("Tür: Game, Movie veya Book")] string type,
        [Description("Tahmini bitirme süresi, DAKİKA cinsinden. Kullanıcı saat söylerse dakikaya çevir.")] int? estimatedMinutes = null,
        [Description("Türler, ör. rpg, sci-fi, roman")] string[]? genres = null,
        [Description("Ruh hali etiketleri: chill, focused, emotional, adventurous")] string[]? moodTags = null,
        [Description("Öncelik, 1 (düşük) ile 5 (yüksek) arası")] int priority = 3)
    {
        return api.AddItemAsync(new
        {
            title,
            type,
            estimatedMinutes,
            genres = genres ?? [],
            moodTags = moodTags ?? [],
            priority
        });
    }

    [McpServerTool(Name = "update_status")]
    [Description("Bir öğenin durumunu değiştirir. Kullanıcı bir şeye başladığını, bitirdiğini veya bıraktığını söylediğinde kullan. " +
                 "Öğenin id'sini bilmiyorsan önce search_backlog ile bul.")]
    public static Task<string> UpdateStatus(
        BacklogApiClient api,
        [Description("Öğenin id'si")] int id,
        [Description("Yeni durum: Backlog, InProgress, Done veya Dropped")] string status)
    {
        return api.UpdateStatusAsync(id, status);
    }
    [McpServerTool(Name = "suggest_for_session")]
    [Description("Kullanıcının ayırabildiği süreye ve ruh haline göre backlog'dan puanlanmış öneri adayları döner. " +
             "Kullanıcı 'ne yapayım', 'ne oynasam/izlesem/okusam', 'X saatim var' gibi şeyler söylediğinde " +
             "search_backlog yerine BU aracı kullan. " +
             "Her adayın score ve reasons alanları vardır; önerini bu gerekçelere dayandır, gerekçe uydurma. " +
             "Kullanıcıya bir ana öneri ve en fazla iki alternatif sun. Liste boşsa bunu dürüstçe söyle.")]
    public static Task<string> SuggestForSession(
    BacklogApiClient api,
    [Description("Kullanıcının ayırabildiği süre, DAKİKA cinsinden. Örnek: 'iki saat' = 120.")] int availableMinutes,
    [Description("Ruh hali. chill: yorgun, kafa dinlemek istiyor. focused: odaklı, kafa yoran bir şey istiyor. " +
                 "emotional: duygusal bir şey istiyor. adventurous: heyecan, macera istiyor. " +
                 "Kullanıcı ruh halinden bahsetmediyse boş bırak.")] string? mood = null,
    [Description("Kullanıcı özellikle bir tür istediyse: Game, Movie veya Book. İstemediyse boş bırak.")] string? type = null,
    [Description("Kaç aday dönsün, varsayılan 5")] int limit = 5)
    {
        return api.GetSuggestionsAsync(availableMinutes, mood, type, limit);
    }
    [McpServerTool(Name = "get_stale_items")]
    [Description("Uzun süredir ertelenmiş öğeleri listeler. " +
             "category alanı NeverStarted ise hiç başlanmamış, Abandoned ise yarım bırakılmış demektir. " +
             "Kullanıcı neleri ertelediğini, unuttuğunu veya yarım bıraktığını sorduğunda kullan.")]
    public static Task<string> GetStaleItems(
    BacklogApiClient api,
    [Description("En az kaç gündür dokunulmamış olsun, varsayılan 90")] int olderThanDays = 90,
    [Description("En fazla kaç öğe dönsün, varsayılan 10")] int limit = 10)
    {
        return api.GetStaleItemsAsync(olderThanDays, limit);
    }
}