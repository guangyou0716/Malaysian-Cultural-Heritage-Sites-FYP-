using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PenangGoAppFresh.Services;

namespace PenangGoAppFresh.Auth
{
    /// <summary>
    /// Attaches the current access token to every request; on a 401 and when a refresh
    /// token exists, refreshes (rotating the refresh token) and retries the request once.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ITokenStore _tokenStore;
        private readonly IOptions<ApiOptions> _apiOptions;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public AuthHeaderHandler(ITokenStore tokenStore, IOptions<ApiOptions> apiOptions)
        {
            _tokenStore = tokenStore;
            _apiOptions = apiOptions;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Buffer the body up front so the request can be re-sent after a token refresh
            byte[]? requestBody = null;
            if (request.Content != null)
                requestBody = await request.Content.ReadAsByteArrayAsync(cancellationToken);

            var accessToken = _tokenStore.AccessToken;
            if (!string.IsNullOrWhiteSpace(accessToken) && request.Headers.Authorization == null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized || request.Properties.ContainsKey("RetriedAfterRefresh"))
                return response;

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return response;

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (!await TryRefreshAsync(refreshToken, cancellationToken))
                    return response;

                response.Dispose();

                var clone = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var header in request.Headers)
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (requestBody != null)
                {
                    clone.Content = new ByteArrayContent(requestBody);
                    foreach (var header in request.Content!.Headers)
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
                    clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
                clone.Properties["RetriedAfterRefresh"] = true;

                return await base.SendAsync(clone, cancellationToken);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<bool> TryRefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = new Uri(_apiOptions.Value.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(15)
                };

                var payload = JsonSerializer.Serialize(new { refreshToken });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("api/auth/refresh", content, cancellationToken);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var access = root.TryGetProperty("accessToken", out var a) ? a.GetString() : null;
                var newRefresh = root.TryGetProperty("refreshToken", out var r) ? r.GetString() : null;
                if (string.IsNullOrWhiteSpace(access)) return false;

                await _tokenStore.SaveTokensAsync(access, newRefresh, _tokenStore.SessionToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}