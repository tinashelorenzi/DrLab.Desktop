using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DrLab.Desktop;
using LIMS.Models.Messaging;

namespace LIMS.Services
{
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _authToken;

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Authentication
        public async Task<AuthResponse> LoginAsync(string username, string password)
        {
            var loginData = new { username, password };
            var response = await PostAsync<AuthResponse>("api/auth/login/", loginData);

            if (response != null)
            {
                SetAuthToken(response.Access);
            }

            return response;
        }

        // User Management
        public async Task<UserKeyPair> GetUserKeyPairAsync(string userId)
        {
            return await GetAsync<UserKeyPair>($"api/users/{userId}/keypair/");
        }

        public async Task<bool> SetupUserEncryptionAsync(string password)
        {
            var data = new { password };
            var response = await PostAsync<object>("api/messaging/setup-encryption/", data);
            return response != null;
        }

        // Conversations
        public async Task<List<ConversationModel>> GetConversationsAsync()
        {
            var conversations = await GetAsync<List<ConversationDto>>("api/messaging/conversations/");
            return conversations?.ConvertAll(ConvertToConversationModel) ?? new List<ConversationModel>();
        }

        public async Task<ConversationModel> CreateConversationAsync(List<string> participantIds, string conversationType = "direct", string name = null)
        {
            var data = new
            {
                participants = participantIds,
                type = conversationType,
                name
            };

            var conversation = await PostAsync<ConversationDto>("api/messaging/conversations/create/", data);
            return conversation != null ? ConvertToConversationModel(conversation) : null;
        }

        public async Task<List<ConversationParticipant>> GetConversationParticipantsAsync(string conversationId)
        {
            return await GetAsync<List<ConversationParticipant>>($"api/messaging/conversations/{conversationId}/participants/");
        }

        public async Task<string> GetConversationKeyAsync(string conversationId)
        {
            var response = await GetAsync<ConversationKeyResponse>($"api/messaging/conversations/{conversationId}/key/");
            return response?.EncryptedKey;
        }

        public async Task<bool> UpdateConversationKeysAsync(string conversationId, Dictionary<string, string> encryptedKeys)
        {
            var data = new { encrypted_keys = encryptedKeys };
            var response = await PostAsync<object>($"api/messaging/conversations/{conversationId}/keys/", data);
            return response != null;
        }

        // Messages
        public async Task<List<MessageModel>> GetMessagesAsync(string conversationId, int page = 1, int pageSize = 50)
        {
            var messages = await GetAsync<List<MessageDto>>($"api/messaging/conversations/{conversationId}/messages/?page={page}&page_size={pageSize}");
            return messages?.ConvertAll(ConvertToMessageModel) ?? new List<MessageModel>();
        }

        public async Task<MessageModel> SendMessageAsync(string conversationId, string encryptedContent, string messageType = "text", string replyToId = null)
        {
            var data = new
            {
                conversation_id = conversationId,
                encrypted_content = encryptedContent,
                message_type = messageType,
                reply_to_id = replyToId
            };

            var message = await PostAsync<MessageDto>("api/messaging/messages/send/", data);
            return message != null ? ConvertToMessageModel(message) : null;
        }

        public async Task<bool> MarkMessageAsReadAsync(string messageId)
        {
            var response = await PostAsync<object>($"api/messaging/messages/{messageId}/mark-read/", new { });
            return response != null;
        }

        // File uploads
        public async Task<FileUploadResponse> UploadFileAsync(string filePath, string fileName)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            form.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/messaging/files/upload/", form);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FileUploadResponse>(json, GetJsonOptions());
            }

            return null;
        }

        public async Task<byte[]> DownloadFileAsync(string fileId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/messaging/files/{fileId}/download/");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }

        // Search
        public async Task<List<UserModel>> SearchUsersAsync(string query)
        {
            var users = await GetAsync<List<UserDto>>($"api/users/search/?q={Uri.EscapeDataString(query)}");
            return users?.ConvertAll(ConvertToUserModel) ?? new List<UserModel>();
        }

        // Generic HTTP methods
        private async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, GetJsonOptions());
                }

                await HandleErrorResponse(response);
                return default(T);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GET request failed: {ex.Message}");
                return default(T);
            }
        }

        private async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(responseJson, GetJsonOptions());
                }

                await HandleErrorResponse(response);
                return default(T);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POST request failed: {ex.Message}");
                return default(T);
            }
        }

        private async Task HandleErrorResponse(HttpResponseMessage response)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"API Error {response.StatusCode}: {errorContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token expired, might need to refresh
                throw new UnauthorizedAccessException("Authentication required");
            }
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };
        }

        // Model conversion methods
        private ConversationModel ConvertToConversationModel(ConversationDto dto)
        {
            return new ConversationModel
            {
                Id = dto.Id,
                Name = dto.Name,
                ConversationType = dto.ConversationType,
                LastMessageTime = dto.UpdatedAt,
                LastMessagePreview = dto.LastMessagePreview ?? "",
                UnreadCount = dto.UnreadCount,
                IsActive = false
            };
        }

        private MessageModel ConvertToMessageModel(MessageDto dto)
        {
            return new MessageModel
            {
                Id = dto.Id,
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                SenderName = dto.SenderName,
                EncryptedContent = dto.EncryptedContent,
                MessageType = dto.MessageType,
                Timestamp = dto.CreatedAt,
                IsSentByCurrentUser = dto.SenderId == App.CurrentUserId,
                IsDecrypted = false
            };
        }

        private UserModel ConvertToUserModel(UserDto dto)
        {
            return new UserModel
            {
                Id = dto.Id,
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // DTOs for API communication
    public class AuthResponse
    {
        public string Access { get; set; }
        public string Refresh { get; set; }
        public UserDto User { get; set; }
    }

    public class ConversationDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ConversationType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string LastMessagePreview { get; set; }
        public int UnreadCount { get; set; }
        public List<UserDto> Participants { get; set; }
    }

    public class MessageDto
    {
        public string Id { get; set; }
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string EncryptedContent { get; set; }
        public string MessageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReplyToId { get; set; }
        public string FileName { get; set; }
        public long? FileSize { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class ConversationKeyResponse
    {
        public string EncryptedKey { get; set; }
    }

    public class FileUploadResponse
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string DownloadUrl { get; set; }
    }
}