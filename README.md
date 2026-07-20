<div align="center">

# 🚀 UzayBank

**Modern fintech web uygulaması** — VakıfBank yaz stajı kapsamında geliştirilmiştir.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![SQL Server](https://img.shields.io/badge/MSSQL-Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-8-68217A)
![Leaflet](https://img.shields.io/badge/Leaflet-Maps-199900?logo=leaflet&logoColor=white)
![i18n](https://img.shields.io/badge/i18n-TR%20%2F%20EN-informational)
![License](https://img.shields.io/badge/Ama%C3%A7-Staj%20%2B%20Akademik-blue)

*Clean Architecture · JWT Authentication · Rol Tabanlı Yetkilendirme · Hibrit Hesap Modeli · Çok Dilli · VakıfBank Open Banking · Blockchain-Ready*

</div>

---

## 📋 Proje Hakkında

UzayBank; kullanıcıların banka hesaplarını görüntüleyebildiği, hesap hareketlerini takip edebildiği, finansal analizlerini inceleyebildiği, güncel piyasa verilerine ve en yakın ATM/şube bilgilerine erişebildiği tam kapsamlı (full-stack) bir fintech web uygulamasıdır.

Uygulama **hibrit bir hesap modeli** kullanır:

- **Diğer banka hesapları** — **VakıfBank Open Banking API'leri** üzerinden canlı olarak çekilir; veri kopyalanmaz, her istekte bankadan alınır
- **UzayBank hesapları** — uygulamanın kendi veritabanında tutulur; kullanıcı hesap açabilir, para yatırıp çekebilir ve IBAN ile transfer yapabilir

**Rol tabanlı yetkilendirme** ile yönetici ve kullanıcı ayrımı yapılır; yöneticiler banka hesaplarını kullanıcılara atar, kullanıcılar yalnızca kendilerine atanmış hesapları görür. Arayüz **Türkçe ve İngilizce** olmak üzere iki dili destekler.

Proje ayrıca, lisans bitirme tezi kapsamında **blockchain tabanlı veri bütünlüğü** (SHA-256 hash + Ethereum) ve **Zero Trust erişim kontrolü** (akıllı kontratlar) mimarisiyle genişletilmek üzere tasarlanmıştır.

## 🛠 Teknolojiler

### Backend
| Teknoloji | Kullanım Amacı |
|-----------|----------------|
| **ASP.NET Core 8.0** | Web API |
| **Entity Framework Core 8** | ORM |
| **MSSQL Server** | Veritabanı |
| **JWT Bearer Authentication** | Kimlik doğrulama |
| **BCrypt.Net** | Şifre hash'leme |
| **AutoMapper** | Entity ↔ DTO dönüşümü |
| **IMemoryCache** | Token ve hesap önbellekleme |

### Frontend
| Teknoloji | Kullanım Amacı |
|-----------|----------------|
| **Angular 21** | SPA framework |
| **Chart.js** | Veri görselleştirme |
| **Leaflet + OpenStreetMap** | Harita ve konum servisleri |
| **ngx-translate** | Çok dilli yapı (TR/EN) |
| **SCSS** | Stil |
| **RxJS** | Asenkron veri akışı |

### VakıfBank Open Banking Entegrasyonu

| Servis | Endpoint | Yetkilendirme |
|--------|----------|---------------|
| Hesap listesi | `/accountList` | B2B Credentials |
| Hesap hareketleri | `/accountTransactions` | B2B Credentials |
| En yakın şube/ATM | `/getNearestBranchATM` | Client Credentials |
| Döviz kurları | `/getCurrencyRates` | Client Credentials |
| Altın fiyatları | `/getGoldPrices` | Client Credentials |

> ⛓️ *(Planlanan)* **Ethereum / Solidity** — işlem bütünlüğü için blockchain katmanı (tez entegrasyonu)

## 🏗 Mimari

Proje **Clean Architecture** prensiplerine göre 4 katmanlı yapıdadır:

```
UzayBank/
├── src/
│   ├── UzayBank.API/              → HTTP endpoint'leri (Controllers, Middleware)
│   ├── UzayBank.Application/      → İş mantığı (Services, DTOs, Interfaces, Mappings)
│   ├── UzayBank.Domain/           → Çekirdek varlıklar (Entities, Enums, Interfaces)
│   └── UzayBank.Infrastructure/   → Dış dünya (EF Core, VakıfBank API, Persistence)
└── client/                        → Angular frontend
```

**Bağımlılık yönü:**

```
API ──────► Application ──────► Domain
 │                                ▲
 └────────► Infrastructure ───────┘
```

Domain hiçbir katmana bağımlı değildir. Bu tasarım sayesinde veri kaynağı (MSSQL ↔ VakıfBank API) **tek satırlık DI kaydı değişikliğiyle** değiştirilebilir — Controller ve Application katmanlarına dokunulmaz.

### Hibrit Hesap Modeli

| | Diğer Banka Hesapları | UzayBank Hesapları |
|---|---|---|
| **Kaynak** | VakıfBank Open Banking API | Uygulamanın kendi veritabanı |
| **Veri tazeliği** | Her istekte canlı çekilir | Tek doğruluk kaynağı veritabanı |
| **Erişim** | Yönetici tarafından atanır | Kullanıcı kendi açar |
| **İşlem** | Yalnızca görüntüleme | Para yatırma, çekme, transfer |

Bakiye bilgisi **hiçbir zaman kopyalanmaz** — banka hesaplarının bakiyesi her istekte API'den alınır, böylece veri bayatlaması önlenir.

## ✨ Özellikler

- 🔐 **JWT tabanlı kimlik doğrulama** — kayıt, giriş, oturum yönetimi
- 🚪 **Güvenli çıkış (token iptali)** — çıkış yapılan token sunucu tarafında kara listeye alınır; JWT'nin *stateless* yapısı nedeniyle oturum kapatıldıktan sonra token'ın hâlâ geçerli kalması sorunu çözülür
- 👥 **Rol tabanlı yetkilendirme** — yönetici (Admin) ve kullanıcı (Customer) ayrımı; rol bilgisi JWT içinde taşınır
- 🗂 **Yönetici paneli** — yöneticiler banka hesaplarını kullanıcılara atar/kaldırır; her hesap yalnızca tek bir kullanıcıya atanabilir
- 🏦 **UzayBank hesapları** — kullanıcı kendi hesabını açar; hesap numarası ve IBAN otomatik üretilir
- 💸 **Para yatırma / çekme / transfer** — IBAN ile hesaplar arası transfer; yetersiz bakiye, geçersiz alıcı ve aynı hesap kontrolleri
- 🏢 **Kurumsal e-posta kısıtı** — yalnızca `@uzaybank.com` adresleriyle kayıt
- 🌐 **Çok dilli arayüz (TR/EN)** — anlık dil değişimi, tercih tarayıcıda saklanır; arka uç hata *kodları* döndürür, çeviri istemcide yapılır
- 📊 **Dashboard** — banka ve UzayBank hesapları ayrı bölümlerde, toplam bakiye özeti
- 💱 **Piyasa şeridi** — canlı döviz kurları (USD, EUR, GBP) ve gram altın fiyatı
- 📈 **Analiz sayfası** — bakiye değişim grafiği, gelir/gider dağılımı (Chart.js) ve altında gelen/giden/net toplamları; tüm hesapların işlemleri birleştirilerek analiz edilir
- 💳 **Hesap detayı** — IBAN, bakiye, tarih filtreli işlem geçmişi
- 🗺 **En yakın ATM ve şubeler** — konum tabanlı harita, tür ve mesafe filtreleri
- 💪 **Şifre gücü göstergesi** — kayıt sırasında gerçek zamanlı (Zayıf → Çok Güçlü)

### 🔒 Güvenlik Önlemleri

| Önlem | Açıklama |
|-------|----------|
| BCrypt | Şifreler geri döndürülemez şekilde hash'lenir |
| Token kara listesi | Çıkış yapılan token'lar iptal edilir; her istekte `OnTokenValidated` kancasıyla kontrol edilir |
| Rol tabanlı erişim | Yönetici uç noktaları `[Authorize(Roles = "Admin")]` ile korunur |
| IDOR koruması | `userId` istemciden değil, JWT token'dan okunur |
| Sahiplik kontrolü | Kullanıcı yalnızca kendisine atanmış hesaba erişebilir → aksi hâlde `403 Forbid` |
| Atomik transfer | Para transferi veritabanı transaction'ı içinde yürütülür; hata durumunda tüm işlem geri alınır |
| Kullanıcı sayımı (enumeration) koruması | Giriş hatası "kullanıcı yok" / "şifre yanlış" ayrımı yapmadan tek mesaj döndürür |
| Auth Guard | Frontend'de korumalı route'lar |
| Admin Guard | Yönetici sayfaları yalnızca Admin rolüne açıktır |
| 401 Interceptor | Oturum süresi dolduğunda otomatik yönlendirme |
| User Secrets | Hassas bilgiler repo'ya gitmez |

### ⚡ Performans

- **Hesap listesi önbelleği** — VakıfBank API'ye gereksiz istek gitmemesi için 2 dakikalık bellek içi önbellekleme
- **Token önbelleği** — B2B ve Client Credentials token'ları ayrı ayrı önbelleklenir
- **Paralel işlem sorgusu** — analiz sayfasında tüm hesapların hareketleri eşzamanlı (paralel) çekilir

## 🚀 Kurulum

### Gereksinimler

- .NET 8 SDK
- Node.js 20+ ve Angular CLI
- SQL Server (Express yeterli)
- Visual Studio 2022 / VS Code

### 1️⃣ Backend

```bash
# Repo'yu klonla
git clone https://github.com/yagmursultanekin/UzayBank.git
cd UzayBank

# User Secrets'ı yapılandır (UzayBank.API klasöründe)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=UzayBankDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SecretKey" "guclu-bir-secret-key-en-az-32-karakter"
dotnet user-secrets set "Jwt:Issuer" "UzayBankAPI"
dotnet user-secrets set "Jwt:Audience" "UzayBankClient"
dotnet user-secrets set "Jwt:ExpiryInMinutes" "60"

# Veritabanını oluştur
dotnet ef database update --project src/UzayBank.Infrastructure --startup-project src/UzayBank.API

# Çalıştır
dotnet run --project src/UzayBank.API
```

> Backend `https://localhost:7100` adresinde çalışır. Swagger arayüzü: `https://localhost:7100/swagger`

### 2️⃣ VakıfBank API (opsiyonel)

VakıfBank entegrasyonu için ek secrets:

```bash
dotnet user-secrets set "VakifBankApi:BaseUrl" "BASE_URL"
dotnet user-secrets set "VakifBankApi:ApiKey" "API_KEY"
dotnet user-secrets set "VakifBankApi:ApiSecret" "API_SECRET"
dotnet user-secrets set "VakifBankApi:Scope" "oob account public"
dotnet user-secrets set "VakifBankApi:Resource" "sandbox"
dotnet user-secrets set "VakifBankApi:ConsentId" "CONSENT_ID"
```

> ⚠️ **BaseUrl:** Developer Portal'daki güncel gateway adresiyle birebir aynı olmalıdır — yanlış temel adres, isteklerin WAF tarafından reddedilmesine neden olur.
>
> ⚠️ **ConsentId:** Rıza numaraları **süreli**dir. Hesap servisleri aniden `ACBG000001` (doğrulama bilgisi geçersiz) hatası vermeye başlarsa, portaldan yeni rıza alıp bu değeri güncelleyin.

Veri kaynağını değiştirmek için `Program.cs`:

```csharp
// MSSQL (seed data ile):
builder.Services.AddScoped<IAccountService, AccountService>();

// VakıfBank API:
builder.Services.AddScoped<IAccountService, VakifBankAccountService>();
```

### 3️⃣ Frontend

```bash
cd client
npm install
npx ng serve
```

> Uygulama `http://localhost:4200` adresinde açılır.

### 4️⃣ Yönetici (Admin) Tanımlama

Uygulamada bir yönetici oluşturmak için önce normal kayıt akışından bir kullanıcı oluşturun (şifrenin doğru şekilde hash'lenmesi için bu adım gereklidir), ardından ilgili kullanıcının rolünü veritabanından yükseltin:

```sql
UPDATE Users SET Role = 1 WHERE Email = 'admin@uzaybank.com';
```

> `Role = 1` → Admin, `Role = 0` → Customer (varsayılan).
> Yönetici olarak giriş yaptıktan sonra dashboard'da **Yönetim Paneli** butonu görünür hâle gelir.

## 📡 API Endpoint'leri

### Kimlik Doğrulama

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|:----:|
| `POST` | `/api/auth/register` | Kullanıcı kaydı (yalnızca `@uzaybank.com`) | — |
| `POST` | `/api/auth/login` | Giriş, JWT token döner | — |
| `POST` | `/api/auth/logout` | Güvenli çıkış (token iptali) | ✅ |

### Banka Hesapları (VakıfBank)

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|:----:|
| `GET` | `/api/account/my-accounts` | Kullanıcıya atanmış banka hesapları | ✅ |
| `GET` | `/api/account/{id}` | Hesap detayı (sahiplik kontrolü) | ✅ |
| `GET` | `/api/account/{id}/transactions?startDate=&endDate=` | Tarih filtreli işlem geçmişi | ✅ |
| `GET` | `/api/account/all-transactions?startDate=&endDate=` | Tüm hesapların birleştirilmiş işlemleri (analiz) | ✅ |

### UzayBank Hesapları

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|:----:|
| `GET` | `/api/uzayaccount/my` | Kullanıcının UzayBank hesapları | ✅ |
| `POST` | `/api/uzayaccount` | Yeni hesap açar (hesap no + IBAN otomatik) | ✅ |
| `GET` | `/api/uzayaccount/{id}/transactions` | Hesabın işlem geçmişi | ✅ |
| `POST` | `/api/uzayaccount/deposit` | Para yatırma | ✅ |
| `POST` | `/api/uzayaccount/withdraw` | Para çekme | ✅ |
| `POST` | `/api/uzayaccount/transfer` | IBAN ile transfer (atomik) | ✅ |

### Piyasa ve Konum

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|:----:|
| `GET` | `/api/branch/nearest?lat=&lng=&distance=` | En yakın ATM ve şubeler | ✅ |
| `GET` | `/api/market/currencies` | Güncel döviz kurları | ✅ |
| `GET` | `/api/market/gold` | Güncel altın fiyatları | ✅ |

### Yönetim

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|:----:|
| `GET` | `/api/admin/assignments` | Tüm hesaplar ve atandıkları kullanıcılar | 🛡 Admin |
| `GET` | `/api/admin/users` | Kullanıcı listesi (atama için) | 🛡 Admin |
| `POST` | `/api/admin/assign` | Bir hesabı bir kullanıcıya atar | 🛡 Admin |
| `POST` | `/api/admin/unassign` | Bir hesabın atamasını kaldırır | 🛡 Admin |

## 🗺 Yol Haritası

- [x] Clean Architecture iskeleti
- [x] JWT authentication + güvenli endpoint'ler
- [x] Güvenli çıkış (token kara listesi)
- [x] Rol tabanlı yetkilendirme + yönetici hesap atama paneli
- [x] Kullanıcı bazlı hesap erişim kontrolü (IDOR koruması)
- [x] Dashboard, analiz sayfası, hesap detayı
- [x] Çok dilli arayüz (Türkçe / İngilizce)
- [x] VakıfBank OAuth 2.0 token altyapısı (B2B + Client Credentials)
- [x] VakıfBank hesap servislerinin tam entegrasyonu
- [x] En yakın ATM/şube haritası (Leaflet + OpenStreetMap)
- [x] Yatırım servisleri (döviz kurları, altın fiyatları)
- [x] Hibrit hesap modeli — UzayBank hesapları, para yatırma/çekme ve IBAN ile transfer
- [ ] **Tez entegrasyonu:** SHA-256 işlem hash'leri + Ethereum (Ganache) + Zero Trust akıllı kontrat erişim kontrolü

> **Kapsam dışı bırakılanlar:** Kredi notu hesaplama ve AI destekli harcama danışmanı modülleri değerlendirilmiş, ancak VakıfBank Open Banking API'sinin bu özellikleri destekleyecek veriyi (kredi skoru servisi / işyeri bazlı harcama açıklaması) sağlamaması nedeniyle bilinçli olarak kapsam dışında bırakılmıştır.

## 🎓 Akademik Bağlam

Bu proje, lisans bitirme tezindeki hibrit mimarinin uygulama zeminidir:

- **Performans:** Ham veriler SQL Server'da tutulur
- **Bütünlük:** Verilerin SHA-256 tabanlı dijital parmak izleri Ethereum blokzincirinde saklanır
- **Erişim kontrolü:** Her istek, akıllı kontratlar üzerinden sorgulanan **Zero Trust** protokolüne dayanır

UzayBank hesaplarındaki işlemler (transfer, para yatırma/çekme) uygulama tarafından üretildiği için, blockchain tabanlı bütünlük doğrulamasının uygulanacağı veri kümesini oluşturur.

## ⚙️ Bilinen Sınırlamalar

- **Token kara listesi bellek tabanlıdır.** Uygulama yeniden başlatıldığında liste sıfırlanır; üretim ortamında kalıcı bir depo (ör. Redis) kullanılmalıdır.
- **Hesap-kullanıcı eşlemesi uygulama tarafında kurulur.** VakıfBank sandbox'ı tek kurumsal kimlik sunduğu için hesaplar kullanıcı bazlı ayrılmamıştır; bu eşleme yönetici paneli üzerinden yapılır. Gerçek ortamda bu bilgi bankadan gelen rıza (consent) kapsamında sağlanır.
- **Yönetici rolü veritabanından atanır.** Uygulama içinden yönetici tanımlama akışı bulunmamaktadır.

## 📄 Lisans

Bu proje staj ve akademik amaçlarla geliştirilmiştir.
