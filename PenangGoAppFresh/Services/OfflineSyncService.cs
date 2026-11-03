using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Maui.Networking;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public enum OfflineSyncState { Unknown, Offline, Syncing, Synced, Conflict }

    /// <summary>
    /// SQLite-backed cache + outbox. The server remains the source of truth:
    /// reads are cache-first with refresh, and only reversible writes (bookmarks)
    /// may enter the outbox. Capacity-sensitive writes (bookings) stay online-only.
    /// </summary>
    public class OfflineSyncService
    {
        private const string LastSyncKey = "last_sync_at";
        private const int SchemaVersion = 1;

        private readonly HttpClient _httpClient;
        private readonly PenangGoOfflineContext _db;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private OfflineSyncState _state = OfflineSyncState.Unknown;

        public OfflineSyncService(HttpClient httpClient, PenangGoOfflineContext db)
        {
            _httpClient = httpClient;
            _db = db;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public event Action? StateChanged;

        public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        public OfflineSyncState State => _state;
        public DateTime? LastSyncTime { get; private set; }
        public int CachedSiteCount => _db.HeritageSites.Count();
        public int CachedEventCount => _db.Events.Count();
        public int OutboxCount => _db.OutboxItems.Count();

        public void Initialize()
        {
            _db.Database.EnsureCreated();
            var meta = _db.SyncMeta.AsNoTracking().FirstOrDefault(m => m.Key == LastSyncKey);
            LastSyncTime = meta != null && DateTime.TryParse(meta.Value, out var t) ? t : (DateTime?)null;
            SetState(IsOnline ? OfflineSyncState.Synced : OfflineSyncState.Offline);
        }

        public async Task InitializeAsync()
        {
            Initialize();
            if (IsOnline)
                await SyncToLocalAsync();
        }

        /// <summary>Pull the server snapshot (approved sites, events, own bookmarks/bookings) into the cache.</summary>
        public async Task SyncToLocalAsync()
        {
            await _syncLock.WaitAsync();
            try
            {
                if (!IsOnline)
                {
                    SetState(OfflineSyncState.Offline);
                    return;
                }

                SetState(OfflineSyncState.Syncing);
                await FlushOutboxAsyncInternal();

                var response = await _httpClient.GetAsync("api/sync/download");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var snapshot = JsonSerializer.Deserialize<SyncSnapshot>(json, _jsonOptions);
                if (snapshot?.Sites == null || snapshot.Events == null)
                {
                    SetState(OfflineSyncState.Conflict);
                    return;
                }

                var now = DateTime.UtcNow;
                using var tx = await _db.Database.BeginTransactionAsync();

                _db.HeritageSites.RemoveRange(_db.HeritageSites);
                foreach (var site in snapshot.Sites)
                {
                    _db.HeritageSites.Add(site);
                    MarkCached(_db.Entry(site), now, site.UpdatedAt);
                }

                _db.Events.RemoveRange(_db.Events);
                foreach (var ev in snapshot.Events)
                {
                    _db.Events.Add(ev);
                    MarkCached(_db.Entry(ev), now, ev.UpdatedAt);
                }

                _db.Bookmarks.RemoveRange(_db.Bookmarks);
                foreach (var bm in snapshot.UserBookmarks ?? new())
                {
                    bm.Site = null!;
                    _db.Bookmarks.Add(bm);
                    MarkCached(_db.Entry(bm), now, bm.CreatedAt);
                }

                _db.Bookings.RemoveRange(_db.Bookings);
                foreach (var bk in snapshot.UserBookings ?? new())
                {
                    bk.User = null!;
                    bk.Event = null!;
                    _db.Bookings.Add(bk);
                    MarkCached(_db.Entry(bk), now, bk.BookingTime);
                }

                UpsertMeta(LastSyncKey, now.ToString("O"));
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                LastSyncTime = now;
                SetState(OfflineSyncState.Synced);
            }
            catch
            {
                // Keep the existing cache on failure; surface the conflict to the UI
                SetState(OfflineSyncState.Conflict);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public async Task<List<HeritageSite>> GetHeritageSitesAsync()
        {
            if (IsOnline)
            {
                try
                {
                    var response = await _httpClient.GetAsync("api/heritagesites");
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var sites = JsonSerializer.Deserialize<List<HeritageSite>>(json, _jsonOptions);
                    if (sites != null)
                    {
                        await ReplaceSitesAsync(sites);
                        return sites;
                    }
                }
                catch { /* fall through to cache */ }
            }
            return await GetCachedSitesAsync();
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            if (IsOnline)
            {
                try
                {
                    var response = await _httpClient.GetAsync("api/events");
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var events = JsonSerializer.Deserialize<List<Event>>(json, _jsonOptions);
                    if (events != null)
                    {
                        var now = DateTime.UtcNow;
                        _db.Events.RemoveRange(_db.Events);
                        foreach (var ev in events)
                        {
                            _db.Events.Add(ev);
                            MarkCached(_db.Entry(ev), now, ev.UpdatedAt);
                        }
                        await _db.SaveChangesAsync();
                        return events;
                    }
                }
                catch { /* fall through to cache */ }
            }
            return await GetCachedEventsAsync();
        }

        public async Task<List<HeritageSite>> GetCachedSitesAsync()
            => await _db.HeritageSites.AsNoTracking().ToListAsync();

        public async Task<List<Event>> GetCachedEventsAsync()
            => await _db.Events.AsNoTracking().ToListAsync();

        // --- Bookmark outbox (reversible writes are the only offline writes) ---

        public void QueueBookmarkAdd(int siteId)
        {
            _db.OutboxItems.Add(new OfflineOutboxItem { Operation = "BookmarkAdd", SiteId = siteId, QueuedAt = DateTime.UtcNow });
            _db.SaveChanges();
            SetState(OfflineSyncState.Offline);
        }

        public bool QueueBookmarkRemove(int bookmarkId)
        {
            var mirror = _db.Bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (mirror == null) return false;

            _db.OutboxItems.Add(new OfflineOutboxItem { Operation = "BookmarkRemove", BookmarkId = bookmarkId, QueuedAt = DateTime.UtcNow });
            _db.Bookmarks.Remove(mirror);
            _db.SaveChanges();
            SetState(OfflineSyncState.Offline);
            return true;
        }

        public int? TryResolveCachedBookmarkId(int siteId)
            => _db.Bookmarks.AsNoTracking().FirstOrDefault(b => b.SiteId == siteId)?.Id;

        public async Task FlushOutboxAsync()
        {
            if (_db.OutboxItems.AsNoTracking().Any() == false) return;
            await _syncLock.WaitAsync();
            try
            {
                if (!IsOnline)
                {
                    SetState(OfflineSyncState.Offline);
                    return;
                }
                SetState(OfflineSyncState.Syncing);
                await FlushOutboxAsyncInternal();
                SetState(OfflineSyncState.Synced);
            }
            catch
            {
                SetState(OfflineSyncState.Conflict);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        public void ClearLocalCache()
        {
            _db.HeritageSites.RemoveRange(_db.HeritageSites);
            _db.Events.RemoveRange(_db.Events);
            _db.Bookmarks.RemoveRange(_db.Bookmarks);
            _db.Bookings.RemoveRange(_db.Bookings);
            _db.OutboxItems.RemoveRange(_db.OutboxItems);
            _db.SyncMeta.RemoveRange(_db.SyncMeta);
            _db.SaveChanges();
            LastSyncTime = null;
            SetState(OfflineSyncState.Unknown);
        }

        // --- internals ---

        private async Task FlushOutboxAsyncInternal()
        {
            var items = await _db.OutboxItems.AsNoTracking().ToListAsync();
            if (items.Count == 0) return;

            var add = items.Where(i => i.Operation == "BookmarkAdd" && i.SiteId.HasValue)
                .Select(i => i.SiteId!.Value).Distinct().ToList();
            var remove = items.Where(i => i.Operation == "BookmarkRemove" && i.BookmarkId.HasValue)
                .Select(i => i.BookmarkId!.Value).Distinct().ToList();

            var payload = JsonSerializer.Serialize(new { bookmarkAddSiteIds = add, bookmarkRemoveIds = remove }, _jsonOptions);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/sync/upload", content);
            response.EnsureSuccessStatusCode();

            _db.OutboxItems.RemoveRange(_db.OutboxItems);
            await _db.SaveChangesAsync();
        }

        private async Task ReplaceSitesAsync(List<HeritageSite> sites)
        {
            var now = DateTime.UtcNow;
            _db.HeritageSites.RemoveRange(_db.HeritageSites);
            foreach (var site in sites)
            {
                _db.HeritageSites.Add(site);
                MarkCached(_db.Entry(site), now, site.UpdatedAt);
            }
            await _db.SaveChangesAsync();
        }

        private void UpsertMeta(string key, string value)
        {
            var meta = _db.SyncMeta.FirstOrDefault(m => m.Key == key);
            if (meta == null)
                _db.SyncMeta.Add(new OfflineSyncMeta { Key = key, Value = value });
            else
                meta.Value = value;
        }

        private void SetState(OfflineSyncState state)
        {
            if (_state != state)
            {
                _state = state;
                StateChanged?.Invoke();
            }
        }

        private static void MarkCached(EntityEntry entry, DateTime cachedAt, DateTime? serverUpdatedAt)
        {
            entry.Property("CachedAt").CurrentValue = cachedAt;
            entry.Property("ServerUpdatedAt").CurrentValue = serverUpdatedAt ?? (object)cachedAt;
            entry.Property("SyncState").CurrentValue = "Synced";
            entry.Property("SchemaVersion").CurrentValue = SchemaVersion;
        }

        private class SyncSnapshot
        {
            public List<HeritageSite>? Sites { get; set; }
            public List<Event>? Events { get; set; }
            public List<Bookmark>? UserBookmarks { get; set; }
            public List<Booking>? UserBookings { get; set; }
            public DateTime? LastSync { get; set; }
        }
    }
}