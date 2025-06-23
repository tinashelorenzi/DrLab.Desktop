using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DrLab.Desktop.Models
{
    public class AuthResult
    {
        [JsonPropertyName("user")]
        public UserDto User { get; set; } = new();

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime LastSeen { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class ConversationDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("conversation_type")]
        public string ConversationType { get; set; } = string.Empty;

        [JsonPropertyName("last_message_time")]
        public DateTime LastMessageTime { get; set; }

        [JsonPropertyName("last_message_preview")]
        public string LastMessagePreview { get; set; } = string.Empty;

        [JsonPropertyName("unread_count")]
        public int UnreadCount { get; set; }

        [JsonPropertyName("participants")]
        public List<UserDto> Participants { get; set; } = new();

        [JsonPropertyName("last_message")]
        public MessageDto? LastMessage { get; set; }
    }

    public class MessageDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [JsonPropertyName("sender_name")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("encrypted_content")]
        public string EncryptedContent { get; set; } = string.Empty;

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; } = "text";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("is_read")]
        public bool IsRead { get; set; }

        [JsonPropertyName("reply_to_id")]
        public string? ReplyToId { get; set; }
    }

    public class FileUploadResponse
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class ConversationKeyResponse
    {
        [JsonPropertyName("encrypted_key")]
        public string EncryptedKey { get; set; } = string.Empty;
    }

    public class ConversationParticipant
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }
    }
}