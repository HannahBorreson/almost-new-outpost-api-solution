# almost-new-outpost-api

The ASP.NET Core Web API for **Almost New 24/7 Outpost**, the Software Design 3 course project. It's a hopeful little merchant shop in a city nature has taken back. Every endpoint answers one question: **can this authenticated user perform this action?**

The React UI lives in the sibling repo `almost-new-outpost-ui`. Set `VITE_USE_MOCKS=false` there and it talks to this API instead of its mock data.

---

## Prerequisites

- **.NET 10 SDK**. Check with `dotnet --version` (it should print `10.0.x`).
- **MySQL Community Server 8.4 LTS**, running as the Windows service `MySQL84` on `localhost:3306`.
- The database built from `database/almost_new_outpost.sql`. See **[database/DATABASE_SETUP.md](database/DATABASE_SETUP.md)** for the full install and setup guide.

## One-time setup (PowerShell)

Run these from the repo folder.

```powershell
# 1. Trust the HTTPS development certificate (once per PC)
dotnet dev-certs https --trust

# 2. The database connection the API uses (the limited outpost_app login)
dotnet user-secrets set --project src/AlmostNewOutpost.Api "ConnectionStrings:Outpost" "Server=localhost;Port=3306;Database=almost_new_outpost;User ID=outpost_app;Password=Outpost_Dev_2026!;"

# 3. A random JWT signing key (64 characters; the API refuses to start without one of at least 32 bytes)
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set --project src/AlmostNewOutpost.Api "Jwt:SigningKey" ([Convert]::ToBase64String($bytes))
```

User-secrets are stored in your Windows profile, not in the repo. If either secret is missing, the API stops at startup with a message telling you which one.

## Run

```powershell
dotnet run --project src/AlmostNewOutpost.Api --launch-profile https
```

- API: https://localhost:7001 (http://localhost:5001 redirects to it)
- Swagger UI: https://localhost:7001/swagger (Development only)
- Health check: https://localhost:7001/api/health returns `{ "status": "ok", "database": "ok" }`

If MySQL isn't reachable, startup logs one error ("Cannot reach almost_new_outpost. Is MySQL84 running…") and the API keeps running, so `/api/health` reports `"database": "unreachable"`.

### Trying it in Swagger

1. `POST /api/auth/login` with `{ "username": "medic", "password": "medic123" }`.
2. Copy `accessToken` from the response, click **Authorize**, and paste it (without the word `Bearer`).
3. Try `GET /api/products`, `POST /api/purchases` with `{ "productId": 12 }` (201) and `{ "productId": 15 }` (403).

### Seeded logins

| Username | Password | Role | Can buy |
| --- | --- | --- | --- |
| citizen | citizen123 | Citizen | Basic |
| scout | scout123 | Scout | Basic, Exploration |
| medic | medic123 | Medic | Basic, Medical |
| security | security123 | Security | Basic, Security |
| admin | admin123 | MerchantAdmin | Everything, plus managing products |

### Weather override (for demos)

The weather comes from `RandomWeatherProvider`, a stand-in for a third-party weather API. Every 5 minutes it picks Clear (55%), Storm, Dust Storm or Radiation Event (15% each). Any condition except Clear raises every price by 25%.

To force a condition while demoing, log in as `admin` and call:

```
PUT    /api/admin/weather/override   { "condition": "DustStorm" }   -> every currentPrice becomes 125% of basePrice
DELETE /api/admin/weather/override                                  -> back to the provider's weather
```

Conditions: `Clear`, `Storm`, `DustStorm`, `RadiationEvent`. The override lasts until it's cleared or the API restarts.

### Using it from the UI

In `almost-new-outpost-ui`, create `.env.development.local` containing:

```
VITE_USE_MOCKS=false
VITE_API_BASE_URL=https://localhost:7001/api
```

Start this API, then run `npm run dev` in the UI. CORS allows only `http://localhost:5173` (set in `Cors:AllowedOrigins` in `appsettings.json`).

---

## Tests

```powershell
dotnet build
dotnet test
```

- **Unit tests** (`tests/AlmostNewOutpost.Tests/Unit`) need no database: pricing, the role/category matrix, login rules, the JWT, request validation, weather, and a check that every seeded password hash in the SQL script really matches `username + "123"`.
- **API tests** (`tests/AlmostNewOutpost.Tests/Api`) start the real API in memory against **`almost_new_outpost_test`**, a separate database the tests build themselves from `database/almost_new_outpost.sql`. Your dev database is never touched: the script runner refuses to run unless every `DROP DATABASE` targets a name ending in `_test`.

### Setting `OUTPOST_TEST_ADMIN_CONNECTION`

Building the test database needs MySQL **root**, because it creates and drops a database. Give the tests a root connection string in one of two ways (never in a committed file):

```powershell
# Option A: user-secrets for the test project (remembered)
dotnet user-secrets set --project tests/AlmostNewOutpost.Tests "OUTPOST_TEST_ADMIN_CONNECTION" "Server=localhost;Port=3306;User ID=root;Password=<your root password>;"

# Option B: an environment variable for this PowerShell window only
$env:OUTPOST_TEST_ADMIN_CONNECTION = "Server=localhost;Port=3306;User ID=root;Password=<your root password>;"
```

If neither is set, the API tests are **skipped** with a message saying how to set it, and the unit tests still run.

How the API tests work:
- The database is built **once per test run**, and the seed data is restored before **every** API test, so tests don't affect each other.
- The API under test still connects as `outpost_app`, never as root.
- The weather provider is replaced with one that is always Clear, so prices are predictable. Tests that need a storm use the admin override.
- Running the script resets `outpost_app`'s password to `Outpost_Dev_2026!`, exactly as running it in Workbench does.

---

## How the code is organised

```
src/AlmostNewOutpost.Api/
  Program.cs          composition root: only calls the extension methods in Extensions/
  Controllers/        HTTP endpoints (Admin/ = MerchantAdmin only)
  Models/
    Entities/         Role, User, Category, Product, Purchase (mirror the tables)
    Rows/             what each stored procedure returns
    Requests/         request bodies and query strings, with validation attributes
    Responses/        the JSON the API sends back (the contract the UI relies on)
    Constants/        role, category, policy and claim names
  Data/               OutpostDbContext, DatabaseErrors (MYSQL_ERRNO values)
  Repositories/       one per table group; the only code that calls stored procedures
  Services/           business rules: AuthService, TokenService, ProductService,
                      PurchaseService, CategoryAccessPolicy (pure), PricingService (pure)
  Weather/            provider interface, random provider, shared state, background refresh
  Authorization/      the CanPurchaseProduct requirement and handler
  Options/            settings classes, validated at startup
  Extensions/         AddOutpost... / UseOutpost... setup methods
tests/AlmostNewOutpost.Tests/
  Unit/               no database
  Api/                real HTTP + MySQL (Infrastructure/, Acceptance/ user stories)
```

### How the stored procedures are called

Every read and write goes through a stored procedure, called from a repository with EF Core's **`Database.SqlQuery<T>`**, using an interpolated string:

```csharp
await _db.Database
    .SqlQuery<ProductRow>($"CALL sp_SearchProducts({search}, {sortBy}, {sortDir})")
    .ToListAsync();
```

- The `{values}` become real `DbParameter`s, so user input is never pasted into SQL text.
- A `CALL` can't be wrapped in a subquery, so the rows are always read first (`ToListAsync`) and then filtered with ordinary LINQ-to-objects (`FirstOrDefault`).
- `sp_DeleteProduct` returns no rows, so it uses `Database.ExecuteSqlAsync`.
- The provider is Oracle's `MySql.EntityFrameworkCore` (Pomelo has no EF Core 10 release).
- The first API test (`RepositoryProofTests`) proves this works against MySQL.
- If a future provider version ever can't run `CALL` this way, the fallback is a small ADO.NET helper using `db.Database.GetDbConnection()` with `CommandType.StoredProcedure`.

The procedures report broken rules with `SIGNAL` errors. `DatabaseErrors` lists their numbers, and the services map them to results that controllers turn into status codes:

| MYSQL_ERRNO | Meaning | HTTP |
| --- | --- | --- |
| 50001 | product not found | 404 |
| 50002 | category not found | 400 (`CategoryId`) |
| 50003 / 1062 | duplicate product name | 409 |
| 50004 | product has purchases | 409 |
| 50005 | user not found | 401 |
| 50006 | invalid price | 500 (would be an API bug) |
| 50010 / 50011 | bad sort value | 400 |

### Archived users

A user is archived by setting `Users.ArchivedAtUtc` to the UTC time it happened; `NULL` means the account is active, and setting it back to `NULL` restores it. `sp_GetUserByUsername` and `sp_GetUserById` skip archived users, so an archived user gets the same 401 "Invalid username or password." as an unknown user, and `GET /api/auth/me` returns 401 for them. Their row and their purchases stay in the database, so purchase history is never lost. A token issued before the user was archived keeps working on other routes until it expires (at most 60 minutes), because only login and `/api/auth/me` check whether the user is archived.

### Security notes worth discussing in class

- **Identity always comes from the token's `sub` claim**, never from the URL or body. Extra fields like `"finalPrice": 1` or `"userId": 1` on a purchase are ignored.
- `canPurchase` in product listings is only a display hint. `POST /api/purchases` checks again with the `CanPurchaseProduct` policy, and both use the same `CategoryAccessPolicy`.
- **Logins don't leak which usernames exist**: an unknown user still costs a full password-hash check, and every failure returns the same message.
- **Secure by default**: a fallback policy requires a signed-in user everywhere; only login and health are `[AllowAnonymous]`.
- **Tokens stay valid until they expire.** Logout only deletes the token in the browser; that's why tokens last 60 minutes.
