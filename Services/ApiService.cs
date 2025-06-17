using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DrLab.Desktop.Models;
using Microsoft.Extensions.Configuration;

namespace DrLab.Desktop.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly UserSessionManager _sessionManager;

        public ApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _sessionManager = UserSessionManager.Instance;

            // Set default timeout
            var timeoutSeconds = configuration.GetValue<int>("ApiSettings:Timeout", 30);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var loginRequest = new LoginRequest
            {
                username = username,
                password = password,
                device_fingerprint = GetDeviceFingerprint(),
                location = "Desktop App"
            };

            var loginUrl = $"{_baseUrl}/api/auth/login/";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(loginUrl, loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Save session
                    _sessionManager.SaveSession(loginResponse);

                    return loginResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Login failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Login request timed out. Please check your connection.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Login failed: {ex.Message}", ex);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var token = _sessionManager.GetAccessToken();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var logoutUrl = $"{_baseUrl}/api/auth/logout/";
                    await _httpClient.PostAsync(logoutUrl, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout API call failed: {ex.Message}");
            }
            finally
            {
                // Always clear local session regardless of API call result
                _sessionManager.ClearSession();
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public void SetAuthorizationHeader()
        {
            var token = _sessionManager.GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private string GetDeviceFingerprint()
        {
            // Simple device fingerprint - you can make this more sophisticated
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.ToString();

            var fingerprint = $"{machineName}-{userName}-{osVersion}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fingerprint));
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}