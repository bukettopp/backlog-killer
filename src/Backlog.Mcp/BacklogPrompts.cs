using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerPromptType]
public static class BacklogPrompts
{
    [McpServerPrompt(Name = "haftalik_temizlik")]
    [Description("Ertelenmiş öğeleri tek tek gözden geçiren haftalık backlog temizliği.")]
    public static string HaftalikTemizlik(
        [Description("Kaç günden eski öğelere bakılsın")] int gun = 90)
    {
        return $"""
            Benimle haftalık backlog temizliği yap.

            1. get_stale_items aracıyla {gun} günden uzun süredir bekleyen öğeleri getir.
            2. Önce kısa bir özet ver: kaç tane hiç başlanmamış, kaç tane yarım bırakılmış öğe var.
            3. Sonra öğeleri TEK TEK sor. Her biri için bana üç seçenek sun:
               - Bu hafta başla (durumu InProgress yap)
               - Kalsın (dokunma)
               - Vazgeç (durumu Dropped yap)
            4. Benim cevabıma göre update_status aracını kullan. Bir sonrakine geçmeden önce cevabımı bekle.
            5. En sonda neleri değiştirdiğini özetle.

            Samimi ve kısa konuş, yargılama.
            """;
    }
}