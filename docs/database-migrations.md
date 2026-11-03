# Database & Migrations Guide

> PenangGo — MySQL via EF Core (Pomelo). This documents setup, schema changes, seeding and recovery.

## Environment variables

| Variable | Purpose |
|---|---|
| `PENANGGO_DB_CONNECTION` | MySQL connection string for the seeder tool and EF design-time tooling, e.g. `server=localhost;database=penang_go;user=<user>;password=<password>` |
| `ConnectionStrings__DefaultConnection` | Connection string for the running API (`appsettings.json` contains an empty placeholder on purpose) |
| `JwtSettings__SecretKey` | JWT signing key (required — the API refuses to start without it). Never commit this. |
| `PENANGGO_ADMIN_PASSWORD` | Optional bootstrap admin password for the seed; falls back to the documented dev-only `Admin123!` account |

Nothing sensitive is stored in `appsettings*.json` or any repo file. Secrets must come from environment variables or user secrets (`PenangGoApi.csproj` has a `UserSecretsId`).

## Backup first

Before any schema-changing migration on a database you care about:

```bash
mysqldump -u <user> -p penang_go > backup_YYYYMMDD.sql
```

## Runtime behavior

- `PenangGoApi/Program.cs` calls `EnsureCreatedAsync()` at startup, then `SeedData.InitializeAsync(...)` seeds users / sites / events / panoramas / bookmarks / reviews **iff the tables are empty**.
- `EnsureCreated` does **not** apply migrations and does **not** alter an existing schema. If the model changes (see below), you must either recreate the local DB or run EF migrations.

To recreate a local dev DB (this applies every new model change):

```bash
# 1. drop the database, 2. restart the API (recreates schema + seeds), or:
dotnet ef database drop -p PenangGoApp.DataAccess -s PenangGoApi   # with PENANGGO_DB_CONNECTION set
```

## Creating a real migration

Requires the `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`) and `PENANGGO_DB_CONNECTION` set (the design-time factory reads it and refuses to run without it):

```bash
set PENANGGO_DB_CONNECTION=server=localhost;database=penang_go;user=<user>;password=<password>
dotnet ef migrations add <Name> -p PenangGoApp.DataAccess -s PenangGoApi
dotnet ef database update -p PenangGoApp.DataAccess -s PenangGoApi
```

Current state: only `20250726124101_InitialCreate` exists. Model changes made after that migration:

1. **`users.RefreshToken` → `users.RefreshTokenHash`** — refresh tokens are stored as SHA-256 hashes (rotated on every refresh).
2. **`bookings.IdempotencyKey`** (string, 64) — idempotent booking creation via the `Idempotency-Key` header.
3. **`audit_logs` table** (`AuditLog` entity) — admin actions (role/status changes, site approve/reject, booking status changes, review moderation, user delete) are recorded with actor, before/after, IP and timestamp. Query via `GET /api/auditlogs` (admin).
4. **`site_visit_bookings` table** (`SiteVisitBooking` entity) + `heritage_sites.RequiresReservation` / `heritage_sites.DailyCapacity` — site-visit reservations via `POST /api/sitevisits` (capacity checked per site/date/time-slot; free — no pricing model).
5. **`reviews.ApprovalStatus`** — new reviews start `Pending`; public `GET /api/reviews/site/{id}` returns approved only; admins moderate via `PUT /api/reviews/{id}/approve|reject`. Site rating aggregates count approved reviews only.
6. **`users.PasswordResetTokenHash` / `PasswordResetTokenExpiresAt`** (hashed, 1-hour expiry) — password reset flow: `POST /api/auth/forgot-password` (enumeration-safe; the reset code is emailed when SMTP is configured, otherwise delivery is skipped with a console notice) → `POST /api/auth/reset-password`. Also `POST /api/auth/change-password` (JWT-authenticated; verifies the current password server-side and revokes refresh tokens).
7. **`users.AccessibilityPreference`** — profile accessibility preference (None/Visual/Hearing/Mobility/Cognitive), editable on the profile page via the `UpdateUserRequest` DTO (sensitive fields stay excluded — guarded by `PenangGoApi.Tests.UpdateUserRequestTests`).

If you keep using `EnsureCreated` (dev), a fresh DB already contains all of the above; existing DBs need recreation or a new migration.

## Seeding

```bash
# Seeder console tool (DatabaseSeeder project)
set PENANGGO_DB_CONNECTION=...
dotnet run --project DatabaseSeeder -- seed     # fill empty tables
dotnet run --project DatabaseSeeder -- clear    # wipe all tables
```

The API itself seeds on startup when tables are empty, so for local dev you usually don't need the console tool.

Dev-only seed accounts (never use in production): `admin@penanggo.com / Admin123!` (override via `PENANGGO_ADMIN_PASSWORD`) and four demo users with password `Password123!`.

## Restore

```bash
mysql -u <user> -p penang_go < backup_YYYYMMDD.sql
```

## Notes

- The old model-level `HasData` admin seed (baked hash + only 2 sites / 1 event) was removed from `PenangGoContext` — runtime seeding now always provides the full demo dataset.
- Indexes: `users.Email` is unique; `bookmarks(UserId, SiteId)` is unique; `site_visit_bookings(HeritageSiteId, VisitDate, Status)` is indexed; `heritage_sites` and `events` are seeded with default index coverage only. Add indexes for `approval/status` and `updatedAt` queries when volume grows.
- **Client offline cache (SQLite)** lives outside this MySQL database: `penanggo_offline.db` in the app data directory (`PenangGoOfflineContext`). It is a cache + outbox only:
  - Mirror tables: `heritage_sites`, `events`, `bookmarks`, `bookings` (shadow columns `ServerUpdatedAt`, `CachedAt`, `SyncState`, `SchemaVersion`).
  - Outbox: `offline_outbox` rows for offline bookmark add/remove (`BookmarkAdd`/`BookmarkRemove`), flushed via `POST /api/sync/upload`; the mirror is refreshed from `GET /api/sync/download`.
  - Clearing it is safe (Settings → Offline Data → Clear); it contains no data that does not exist on the server.

## Tests

`PenangGoApi.Tests` (xUnit) covers password hashing/policy, JWT token issuance/validation/tamper rejection, refresh-token hashing and the ProblemDetails helper:

```bash
dotnet test PenangGoApi/PenangGoApi.sln
```