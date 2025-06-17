using System;
using System.IO;
using System.Net;
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
        private readonly string _baseUrl;
        private readonly UserSessionManager _sessionManager;
        private readonly int _timeout;

        public ApiService(IConfiguration configuration)
        {
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _sessionManager = UserSessionManager.Instance;
            _timeout = configuration.GetValue<int>("ApiSettings:Timeout", 30) * 1000; // Convert to milliseconds
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                // Create the simplest possible login request
                var loginData = new
                {
                    username = username,
                    password = password
                };

                var jsonPayload = JsonSerializer.Serialize(loginData);
                var url = $"{_baseUrl}/api/auth/login/";

                System.Diagnostics.Debug.WriteLine($"=== RAW HTTP LOGIN REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");
                System.Diagnostics.Debug.WriteLine($"JSON: {jsonPayload}");

                // Use WebRequest instead of HttpClient
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.UserAgent = "DrLab-Desktop/1.0";
                request.Timeout = _timeout;
                request.ReadWriteTimeout = _timeout;

                // Write the request body
                var data = Encoding.UTF8.GetBytes(jsonPayload);
                request.ContentLength = data.Length;

                using (var stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                // Get the response
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var responseContent = await reader.ReadToEndAsync();

                    System.Diagnostics.Debug.WriteLine($"=== RAW HTTP RESPONSE ===");
                    System.Diagnostics.Debug.WriteLine($"Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Content: {responseContent}");

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        _sessionManager.SaveSession(loginResponse);
                        return loginResponse;
                    }
                    else
                    {
                        throw new HttpRequestException($"Login failed: {response.StatusCode} - {responseContent}");
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== WEB EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Status: {ex.Status}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");

                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    using (var errorStream = errorResponse.GetResponseStream())
                    using (var errorReader = new StreamReader(errorStream))
                    {
                        var errorContent = await errorReader.ReadToEndAsync();
                        System.Diagnostics.Debug.WriteLine($"Error Response: {errorContent}");
                        throw new HttpRequestException($"Login failed: {errorResponse.StatusCode} - {errorContent}");
                    }
                }
                else
                {
                    throw new Exception($"Network error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var token = _sessionManager.GetAccessToken();
                if (!string.IsNullOrEmpty(token))
                {
                    var url = $"{_baseUrl}/api/auth/logout/";
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.Headers.Add("Authorization", $"Bearer {token}");
                    request.Timeout = _timeout;

                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                        // Logout successful
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout failed: {ex.Message}");
            }
            finally
            {
                _sessionManager.ClearSession();
            }
        }

        public void SetAuthorizationHeader()
        {
            // Not applicable for WebRequest-based implementation
        }

        private string GetDeviceFingerprint()
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.ToString();

            var fingerprint = $"{machineName}-{userName}-{osVersion}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fingerprint));
        }

        public void Dispose()
        {
            // No resources to dispose with WebRequest
        }
    }
}