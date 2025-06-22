using System;
using System.Net.Http;
using System.Text;
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
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8000";
            _sessionManager = UserSessionManager.Instance;

            // Set default timeout
            var timeoutSeconds = configuration.GetValue<int>("ApiSettings:Timeout", 30);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            // Create simple login request matching Django backend expectations
            var loginRequest = new
            {
                username = username,
                password = password
            };

            var loginUrl = $"{_baseUrl}/api/auth/login/";

            try
            {
                // Manually serialize JSON and create StringContent
                var jsonString = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Clear headers and set explicitly
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // Debug: Log the request
                System.Diagnostics.Debug.WriteLine($"Sending login request to: {loginUrl}");
                System.Diagnostics.Debug.WriteLine($"Request JSON: {jsonString}");
                System.Diagnostics.Debug.WriteLine($"Content-Type: application/json");

                var response = await _httpClient.PostAsync(loginUrl, content);

                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Response content: {jsonResponse}");

                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Save session if login response is valid
                    if (loginResponse != null)
                    {
                        _sessionManager.SaveSession(loginResponse);
                    }

                    return loginResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error response: {errorContent}");
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
                throw new Exception($"Unexpected error during login: {ex.Message}", ex);
            }
        }

        private static string GetDeviceFingerprint()
        {
            // Simplified device fingerprint for future use
            return $"{Environment.MachineName}-{Environment.UserName}";
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var logoutUrl = $"{_baseUrl}/api/auth/logout/";
                var token = _sessionManager.GetAccessToken();

                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.PostAsync(logoutUrl, null);

                // Clear session regardless of server response
                _sessionManager.ClearSession();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Clear session even if logout request fails
                _sessionManager.ClearSession();
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}