using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DrLab.Desktop.Models;
using Microsoft.Extensions.Configuration;

namespace DrLab.Desktop.Services
{
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string? _authToken;

        public ApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8000";

            var timeout = configuration.GetValue<int>("ApiSettings:Timeout", 30);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            // Set default headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthToken()
        {
            _authToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/login/", loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        SetAuthToken(loginResponse.Token);
                        return new ApiResponse<LoginResponse>
                        {
                            Success = true,
                            Data = loginResponse,
                            Message = "Login successful"
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = $"Login failed: {response.StatusCode}",
                    Errors = new List<string> { errorContent }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    Success = false,
                    Message = "Login failed due to network error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<bool>> LogoutAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/logout/", null);
                ClearAuthToken();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Logout successful"
                };
            }
            catch (Exception ex)
            {
                ClearAuthToken(); // Clear token even if logout fails
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Logout failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<List<Conversation>>> GetConversationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/messaging/conversations/");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var conversations = JsonSerializer.Deserialize<List<Conversation>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) ?? new List<Conversation>();

                    return new ApiResponse<List<Conversation>>
                    {
                        Success = true,
                        Data = conversations
                    };
                }

                return new ApiResponse<List<Conversation>>
                {
                    Success = false,
                    Message = $"Failed to fetch conversations: {response.StatusCode}",
                    Data = new List<Conversation>()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Conversation>>
                {
                    Success = false,
                    Message = "Failed to fetch conversations",
                    Errors = new List<string> { ex.Message },
                    Data = new List<Conversation>()
                };
            }
        }

        public async Task<ApiResponse<List<Message>>> GetMessagesAsync(string conversationId, int page = 1, int pageSize = 50)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/messaging/conversations/{conversationId}/messages/?page={page}&page_size={pageSize}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var messages = JsonSerializer.Deserialize<List<Message>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) ?? new List<Message>();

                    return new ApiResponse<List<Message>>
                    {
                        Success = true,
                        Data = messages
                    };
                }

                return new ApiResponse<List<Message>>
                {
                    Success = false,
                    Message = $"Failed to fetch messages: {response.StatusCode}",
                    Data = new List<Message>()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Message>>
                {
                    Success = false,
                    Message = "Failed to fetch messages",
                    Errors = new List<string> { ex.Message },
                    Data = new List<Message>()
                };
            }
        }

        public async Task<ApiResponse<Message>> SendMessageAsync(string conversationId, string content, string messageType = "text", string? replyToId = null)
        {
            try
            {
                var messageData = new
                {
                    content = content,
                    message_type = messageType,
                    reply_to = replyToId
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/messaging/conversations/{conversationId}/messages/", messageData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var message = JsonSerializer.Deserialize<Message>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return new ApiResponse<Message>
                    {
                        Success = true,
                        Data = message,
                        Message = "Message sent successfully"
                    };
                }

                return new ApiResponse<Message>
                {
                    Success = false,
                    Message = $"Failed to send message: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Message>
                {
                    Success = false,
                    Message = "Failed to send message",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<List<User>>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }) ?? new List<User>();

                    return new ApiResponse<List<User>>
                    {
                        Success = true,
                        Data = users
                    };
                }

                return new ApiResponse<List<User>>
                {
                    Success = false,
                    Message = $"Failed to fetch users: {response.StatusCode}",
                    Data = new List<User>()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<User>>
                {
                    Success = false,
                    Message = "Failed to fetch users",
                    Errors = new List<string> { ex.Message },
                    Data = new List<User>()
                };
            }
        }

        public async Task<ApiResponse<bool>> MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/messaging/messages/{messageId}/read/", null);

                return new ApiResponse<bool>
                {
                    Success = response.IsSuccessStatusCode,
                    Data = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Message marked as read" : $"Failed to mark message as read: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to mark message as read",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}