using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class OfflineSyncService
    {
        private readonly PenangGoContext _remote;
        private readonly PenangGoOfflineContext _local;
        public OfflineSyncService(PenangGoContext remote, PenangGoOfflineContext local)
        {
            _remote = remote;
            _local = local;
        }

        public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        public async Task SyncToLocalAsync()
        {
            if (!IsOnline) return;
            _local.HeritageSites.RemoveRange(_local.HeritageSites);
            _local.Events.RemoveRange(_local.Events);
            await _local.SaveChangesAsync();
            _local.HeritageSites.AddRange(await _remote.HeritageSites.ToListAsync());
            _local.Events.AddRange(await _remote.Events.ToListAsync());
            await _local.SaveChangesAsync();
        }

        public async Task<List<HeritageSite>> GetHeritageSitesAsync()
        {
            if (IsOnline)
                return await _remote.HeritageSites.ToListAsync();
            else
                return await _local.HeritageSites.ToListAsync();
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            if (IsOnline)
                return await _remote.Events.ToListAsync();
            else
                return await _local.Events.ToListAsync();
        }
    }
} 