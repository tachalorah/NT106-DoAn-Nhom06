using System;
using System.Net.Http;

namespace SecureChat.Client.Services.Api
{
    public static class ApiClient
    {
        private const string DefaultBaseUrl = "https://localhost:5001/";

        public static HttpClient Create(string? baseUrl = null)
        {
            var resolvedBaseUrl = baseUrl
                ?? Environment.GetEnvironmentVariable("SECURECHAT_API_BASE_URL")
                ?? DefaultBaseUrl;

            return new HttpClient
            {
                BaseAddress = new Uri(resolvedBaseUrl, UriKind.Absolute)
            };
        }
    }
}
