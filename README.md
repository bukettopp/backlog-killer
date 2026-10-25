# Backlog Killer

**Backlog Killer: Oynayacağım, izleyeceğim, okuyacağım deyip biriktirdiklerimi bitirmeme yardım eden MCP asistanı.**

Bu proje, **Model Context Protocol (MCP)** öğrenmek için yaptığım bir çalışma. Amacım bir MCP sunucusunun uçtan uca nasıl kurulduğunu görmekti: araçların (tools) ve prompt'ların nasıl tanımlandığını, Claude'un bunları nasıl seçip çağırdığını ve MCP katmanının gerçek bir backend'in önünde nasıl durduğunu. Konu olarak da hepimizin bildiği bir sorunu seçtim: "Sonra oynarım" diye biriken oyunlar, filmler ve kitaplar.

Claude'a "Bu akşam 2 saatim var, ne yapayım?" ya da "Neleri uzun zamandır erteliyorum?" diye sorduğunuzda, Claude backlog'unuza bakıp gerekçesiyle birlikte cevap veriyor.

---

## Mimari

```
 ┌──────────────┐   stdio (JSON-RPC)   ┌──────────────┐     HTTP      ┌──────────────┐   EF Core   ┌──────────────┐
 │    Claude    │ ───────────────────► │ Backlog.Mcp  │ ────────────► │ Backlog.Api  │ ──────────► │ PostgreSQL   │
 │ (BacklogChat)│ ◄─────────────────── │ (MCP sunucu) │ ◄──────────── │ (Minimal API)│ ◄────────── │ 17 (Docker)  │
 └──────────────┘                      └──────────────┘ localhost:5080└──────────────┘             └──────┬───────┘
                                        ince adaptör:                  iş kuralları,                      │
                                        tool → HTTP isteği             doğrulama, puanlama                │
                                                                                                          │
 ─────────────────────────────── yalnızca geliştirme sırasında ──────────────────────────────────────────┤
                                                                                                          │
 ┌──────────────┐   stdio              ┌───────────────────────────────────┐   SQL (salt okunur)          │
 │ Claude Code  │ ───────────────────► │ postgres MCP                      │ ─────────────────────────────┘
 │(BacklogKiller│                      │ crystaldba/postgres-mcp (Docker)  │
 │  klasörü)    │                      │ --access-mode=restricted          │
 └──────────────┘                      └───────────────────────────────────┘
```

- **Kullanım:** Claude yalnızca `Backlog.Mcp` ile konuşur. `Backlog.Mcp` her aracı `Backlog.Api`'ye bir HTTP isteğine çevirir. Veritabanına yalnızca API erişir.
- **Geliştirme:** Kodu yazarken Claude Code, salt okunur bir Postgres MCP üzerinden tabloları okuyabilir. Şemayı kontrol etmek, seed verisine bakmak ya da bir sorgunun sonucunu doğrulamak için kullanılır. `restricted` modunda bu bağlantıdan veri değiştirilemez.

---

## Tasarım kararı: Claude neden veritabanına doğrudan bağlanmıyor?

Claude'a bir Postgres MCP verip "tabloyu sen yönet" demek mümkündü. Bunu bilerek yapmadım:

1. **İş kuralları backend'de yaşar.** Bir öğenin durumu değiştiğinde `LastTouchedAt` güncellenir, `Done` olunca `CompletedAt` set edilir (`BacklogItem.ChangeStatus`). Öncelik 1 ile 5 arasında olmalı, başlık en fazla 200 karakter olabilir, yeni öğe her zaman `Backlog` durumunda başlar. Öneri puanlaması ve "ertelenmiş" tanımı da API'deki servislerde durur. Claude SQL yazsaydı bu kuralları her seferinde doğru hatırlaması gerekirdi. Şimdi kurallar tek bir yerde ve test edilebilir durumda.
2. **Güvenlik.** Dil modelinin eline sınırsız SQL yetkisi vermek, yanlış anlaşılan tek bir cümlenin `DELETE` ya da `UPDATE` ile sonuçlanabileceği anlamına gelir. MCP üzerinden yalnızca tanımlı işlemler yapılabilir: listeleme, ekleme, durum değiştirme, öneri alma. Örneğin silme aracı bilerek yok. Geliştirme sırasında kullanılan Postgres MCP de `--access-mode=restricted` ile salt okunur çalışır.
3. **MCP ince bir adaptördür.** `Backlog.Mcp` içinde iş mantığı yok. Her araç bir HTTP çağrısı yapar ve cevabı Claude'un okuyabileceği metne çevirir. Aynı API'yi yarın bir web arayüzü, bir mobil uygulama ya da başka bir yapay zekâ istemcisi de kullanabilir. MCP sadece Claude'a açılan bir kapı.

---

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Çalışma zamanı | .NET 9 |
| API | ASP.NET Core Minimal API (+ OpenAPI, Scalar) |
| Veri erişimi | EF Core 9 + Npgsql (snake_case isimlendirme) |
| Veritabanı | PostgreSQL 17 (Docker) |
| MCP sunucusu | [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (stdio transport) |

---

## Proje yapısı

```
BacklogKiller/
├── BacklogKiller.sln
├── docker-compose.yml              # PostgreSQL 17
├── global.json                     # .NET SDK sürümü
├── .config/dotnet-tools.json       # dotnet-ef (local tool)
└── src/
    ├── Backlog.Api/                # Backend: iş kuralları + veritabanı
    │   ├── Program.cs
    │   ├── Backlog.Api.http        # Elle test için örnek istekler
    │   ├── Contracts/              # İstek/cevap modelleri (DTO)
    │   ├── Data/
    │   │   ├── BacklogDbContext.cs
    │   │   ├── DevelopmentSeeder.cs    # Development'ta örnek veri
    │   │   ├── Configurations/         # EF Core tablo yapılandırması
    │   │   └── Migrations/
    │   ├── Endpoints/
    │   │   ├── ItemEndpoints.cs        # /items, /items/stale
    │   │   └── SuggestionEndpoints.cs  # /suggestions
    │   ├── Entities/               # BacklogItem, ItemType, ItemStatus
    │   └── Services/
    │       ├── SuggestionService.cs    # Öneri puanlaması
    │       └── StaleItemService.cs     # Ertelenmiş öğeler
    └── Backlog.Mcp/                # MCP sunucusu (ince adaptör)
        ├── Program.cs              # stdio transport, HttpClient
        ├── BacklogApiClient.cs     # API'ye HTTP çağrıları
        ├── BacklogTools.cs         # MCP araçları
        └── BacklogPrompts.cs       # MCP prompt'ları
```

---

## MCP araçları

| Araç | Ne yapar | Parametreler | API karşılığı |
|---|---|---|---|
| `search_backlog` | Backlog'u listeler ve arar. Bir öğenin id'sini bulmak için de kullanılır. | `type?`, `status?`, `search?` | `GET /items` |
| `add_item` | Yeni oyun, film veya kitap ekler. | `title`, `type`, `estimatedMinutes?`, `genres?`, `moodTags?`, `priority` (varsayılan 3) | `POST /items` |
| `update_status` | Bir öğenin durumunu değiştirir (başladım, bitirdim, bıraktım). | `id`, `status` | `PATCH /items/{id}/status` |
| `suggest_for_session` | Ayrılabilen süreye ve ruh haline göre puanlanmış öneriler döner. | `availableMinutes`, `mood?`, `type?`, `limit` (varsayılan 5) | `GET /suggestions` |
| `get_stale_items` | Uzun süredir dokunulmamış öğeleri listeler (`NeverStarted` / `Abandoned`). | `olderThanDays` (varsayılan 90), `limit` (varsayılan 10) | `GET /items/stale` |

- **Türler:** `Game`, `Movie`, `Book`
- **Durumlar:** `Backlog`, `InProgress`, `Done`, `Dropped`
- **Ruh halleri:** `chill`, `focused`, `emotional`, `adventurous`

### Prompt: `haftalik_temizlik`

Ertelenmiş öğeleri tek tek gözden geçiren bir haftalık temizlik akışı. Parametresi `gun` (varsayılan 90). Claude önce `get_stale_items` ile listeyi getirir ve kaç öğenin hiç başlanmamış, kaçının yarım bırakılmış olduğunu özetler. Sonra her öğe için üç seçenek sunar:

- **Bu hafta başla:** durum `InProgress` olur
- **Kalsın:** dokunulmaz
- **Vazgeç:** durum `Dropped` olur

Her cevaptan sonra `update_status` çağrılır. En sonda yapılan değişiklikler özetlenir.

---

## Öneri algoritması

`SuggestionService` iki adımda çalışır.

**1. Filtreleme (veritabanında)**

- Yalnızca `Backlog` ve `InProgress` durumundaki öğeler aday olur.
- `type` verildiyse o türe göre filtrelenir.
- **Filmler yarıda bırakılmaz:** süresi bilinmeyen ya da ayrılan süreye sığmayan filmler elenir. Oyun ve kitaplar parça parça tüketilebildiği için süreden bağımsız olarak aday kalır.

**2. Puanlama (bellekte)**

| Kural | Puan | Gerekçe metni |
|---|---|---|
| Ruh hali etiketi eşleşiyor | +3 | "Ruh haline uyuyor (chill)" |
| Tahmini süre ayrılan süreye sığıyor | +2 | "Bu akşam bitirebilirsin (116 dk)" |
| Durum `InProgress` | +2 | "Devam ettiğin bir şey" |
| Öncelik | +1 … +5 | "Yüksek / Orta / Düşük öncelikli (x/5)" |
| Bekleme süresi: her 30 gün için +1 (en fazla +3) | +0 … +3 | "183 gündür bekliyor" |

Bekleme süresi `LastTouchedAt` alanından, bu alan boşsa `AddedAt` alanından hesaplanır. Sıralama önce **puana**, eşitlikte **önceliğe**, sonra **id'ye** göre yapılır. Varsayılan limit 5, en fazla 20'dir.

Her öneri `score` ve `reasons` alanlarıyla döner. Araç açıklaması Claude'dan önerisini **bu gerekçelere dayandırmasını**, gerekçe uydurmamasını ve bir ana öneriyle birlikte en fazla iki alternatif sunmasını ister.

---

## Kurulum ve çalıştırma

### Gereksinimler

- .NET 9 SDK
- Docker Desktop
- [Claude Code](https://claude.com/claude-code)

### 1. Şifreyi belirle

Repoda hiçbir şifre yok. Veritabanı şifresi iki yerde, git'e girmeyecek şekilde tutulur:

- **Docker için `.env` dosyası.** Bu dosya `.gitignore` ile hariç tutulur.
- **API için .NET User Secrets.** Bu ayarlar proje klasörünün dışında, kullanıcı profilinizde saklanır.

```bash
cp .env.example .env
# .env içindeki POSTGRES_PASSWORD değerini kendi şifrenizle değiştirin

dotnet user-secrets set "ConnectionStrings:BacklogDb" "Host=localhost;Port=5432;Database=backlog_killer;Username=backlog;Password=<şifreniz>" --project src/Backlog.Api
```

### 2. Veritabanını başlat

```bash
docker compose up -d
```

PostgreSQL 17, `localhost:5432` üzerinde `backlog_killer` veritabanıyla açılır (kullanıcı: `backlog`). `.env` dosyası yoksa `docker compose` hata verip durur.

### 3. Migration'ları uygula

```bash
dotnet tool restore
dotnet ef database update --project src/Backlog.Api
```

### 4. API'yi çalıştır

```bash
dotnet run --project src/Backlog.Api
```

API `http://localhost:5080` adresinde açılır. Development ortamında:

- Tablo boşsa örnek veri otomatik olarak eklenir (`DevelopmentSeeder`).
- API dokümantasyonu `http://localhost:5080/scalar` adresindedir.

### 5. MCP sunucusunu derle

```bash
dotnet build src/Backlog.Mcp
```

MCP sunucusu `--no-build` ile başlatılır. Böylece derleme çıktısı stdout'a yazılmaz ve JSON-RPC hattı bozulmaz. Bu yüzden kodu değiştirdikten sonra tekrar derlemeniz gerekir.

### 6. Claude Code'a bağla

**Sohbet için** (ayrı bir klasör, örneğin `BacklogChat`): Burada Claude yalnızca `backlog-killer` araçlarını görür.

```bash
cd ../BacklogChat
claude mcp add backlog-killer -- dotnet run --project C:\McpDersleri\BacklogKiller\src\Backlog.Mcp --no-build
```

**Geliştirme için** (`BacklogKiller` klasörü): Salt okunur Postgres MCP.

```bash
claude mcp add postgres -e DATABASE_URI=postgresql://backlog:<şifreniz>@host.docker.internal:5432/backlog_killer -- docker run -i --rm -e DATABASE_URI crystaldba/postgres-mcp --access-mode=restricted
```

> Postgres MCP bir Docker konteyneri içinde çalıştığı için veritabanına `localhost` yerine `host.docker.internal` üzerinden bağlanır. `claude mcp add` ayarları repoya değil, kullanıcı profilinizdeki `~/.claude.json` dosyasına yazılır. Yani şifre bu yolla da GitHub'a gitmez.

Bağlantıları kontrol etmek için Claude Code içinde `/mcp` komutunu kullanabilirsiniz. Araçların çalışması için **API'nin açık olması** gerekir. API kapalıysa MCP sunucusu ayakta görünür, ancak araç çağrıları hata verir.

---

## Örnek sohbet

> **Ben:** Bu akşam 2 saatim var ve biraz yorgunum. Ne yapayım?

Claude "2 saat" ifadesini dakikaya, "yorgunum" ifadesini de `chill` ruh haline çevirip aracı çağırır:

```json
suggest_for_session { "availableMinutes": 120, "mood": "chill" }
```

API, seed verisiyle şu adayları puanlayarak döner (kısaltılmış):

```json
[
  { "title": "Disco Elysium",  "score": 8, "reasons": ["Yüksek öncelikli (5/5)", "183 gündür bekliyor"] },
  { "title": "Stardew Valley", "score": 8, "reasons": ["Ruh haline uyuyor (chill)", "Düşük öncelikli (2/5)", "426 gündür bekliyor"] },
  { "title": "Project Hail Mary", "score": 7, "reasons": ["Devam ettiğin bir şey", "Yüksek öncelikli (5/5)"] },
  { "title": "Hades", "score": 6, "reasons": ["Devam ettiğin bir şey", "Yüksek öncelikli (4/5)"] },
  { "title": "Klara and the Sun", "score": 6, "reasons": ["Ruh haline uyuyor (chill)", "Orta öncelikli (3/5)"] }
]
```

> **Claude:** Yorgun olduğun için kafa yormayan bir şey öneririm: **Stardew Valley**. Ruh haline uyuyor ve 426 gündür bekliyor. 2 saat, bir-iki oyun içi sezona başlamak için yeterli.
>
> Alternatifler:
> - **Project Hail Mary:** Zaten okumaya devam ettiğin bir kitap, kaldığın yerden birkaç bölüm okuyabilirsin.
> - **Klara and the Sun:** Sakin ve duygusal bir kitap, o da ruh haline uyuyor.
>
> Disco Elysium aslında en yüksek puanlı aday, ama yoğun okuma ve odak isteyen bir oyun. Onu daha dinç olduğun bir akşama saklamanı öneririm.
>
> Başladığında söylersen Stardew Valley'i "devam ediyor" olarak işaretlerim.

---

## Gelecek planları

- **Steam Web API entegrasyonu:** Kütüphanedeki oyunları ve oynanma sürelerini otomatik içe aktarmak. Veri modelindeki `Source` ve `ExternalId` alanları bunun için hazır.
- **TMDB ve Open Library entegrasyonu:** Film ve kitap eklerken süre, sayfa sayısı ve türleri otomatik doldurmak.
- **İlerleme kaydı (progress log):** "Bugün 45 dk oynadım" gibi oturumları kaydetmek. Kalan süreyi buna göre hesaplamak ve "yarım bırakılmış" tespitini daha akıllı hale getirmek.
- **HTTP transport:** MCP sunucusunu stdio yerine HTTP (Streamable HTTP) üzerinden sunmak. Böylece sunucu uzakta çalışabilir ve birden fazla istemci bağlanabilir.
