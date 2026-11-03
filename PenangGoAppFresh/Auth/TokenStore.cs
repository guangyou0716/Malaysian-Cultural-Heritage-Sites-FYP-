using Microsoft.Maui.Storage;

namespace PenangGoAppFresh.Auth
{
    public interface ITokenStore
    {
        string? AccessToken { get; set; }
        string? SessionToken { get; set; }
        Task SaveTokensAsync(string? accessToken, string? refreshToken, string? sessionToken);
        Task<string?> GetRefreshTokenAsync();
        Task ClearAsync();
    }

    public class TokenStore : ITokenStore
    {
        private const string RefreshTokenKey = "penanggo_refresh_token";
        private string? _refreshToken;

        public string? AccessToken { get; set; }
        public string? SessionToken { get; set; }

        public async Task SaveTokensAsync(string? accessToken, string? refreshToken, string? sessionToken)
        {
            AccessToken = accessToken;
            SessionToken = sessionToken;
            _refreshToken = refreshToken;

            if (refreshToken != null)
            {
                try
                {
                    await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
                }
                catch
                {
                    // SecureStorage can be unavailable (e.g. no KeyStore); fall back to app preferences
                    Preferences.Default.Set(RefreshTokenKey, refreshToken);
                }
            }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            if (_refreshToken != null) return _refreshToken;
            try
            {
                _refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            }
            catch
            {
                _refreshToken = Preferences.Default.Get<string?>(RefreshTokenKey, null);
            }
            return _refreshToken;
        }

        public async Task ClearAsync()
        {
            AccessToken = null;
            SessionToken = null;
            _refreshToken = null;
            try
            {
                SecureStorage.Default.Remove(RefreshTokenKey);
            }
            catch
            {
                Preferences.Default.Remove(RefreshTokenKey);
            }
        }
    }
}
