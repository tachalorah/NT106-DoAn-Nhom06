using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecureChat.Client.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private static ApiClient _instance;

        // Singleton Pattern: Đảm bảo toàn bộ App chỉ dùng chung 1 instance HttpClient
        public static ApiClient Instance => _instance ??= new ApiClient();

        private ApiClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5097/") // Đổi theo cổng của Back-end
            };
        }

        // Lưu JWT Token vào Header cho các request cần xác thực (Chat, Lấy danh sách bạn bè...)
        public void SetAccessToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // Base hàm POST
        public async Task<(bool IsSuccess, TResponse Data, string ErrorMessage)> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseStr = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<TResponse>(responseStr, options);
                    return (true, data, string.Empty);
                }

                return (false, default, $"Lỗi server: {responseStr}");
            }
            catch (Exception ex)
            {
                return (false, default, $"Không thể kết nối máy chủ: {ex.Message}");
            }
        }
    }
}
