using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrLab.Desktop.Models
{
    public class User : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _username = string.Empty;
        private string _displayName = string.Empty;
        private string _email = string.Empty;
        private string _department = string.Empty;
        private bool _isOnline;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class Conversation : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _displayName = string.Empty;
        private string _lastMessage = string.Empty;
        private DateTime _lastMessageTime = DateTime.MinValue;
        private int _unreadCount;
        private bool _isGroup;
        private List<User> _participants = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string LastMessage
        {
            get => _lastMessage;
            set => SetProperty(ref _lastMessage, value);
        }

        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set => SetProperty(ref _lastMessageTime, value);
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        public bool IsGroup
        {
            get => _isGroup;
            set => SetProperty(ref _isGroup, value);
        }

        public List<User> Participants
        {
            get => _participants;
            set => SetProperty(ref _participants, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class Message : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _conversationId = string.Empty;
        private string _senderId = string.Empty;
        private string _senderName = string.Empty;
        private string _content = string.Empty;
        private string _encryptedContent = string.Empty;
        private string _messageType = "text";
        private DateTime _timestamp = DateTime.Now;
        private bool _isRead;
        private bool _isSentByCurrentUser;
        private string? _replyToId;
        private string? _fileName;
        private string? _fileUrl;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string ConversationId
        {
            get => _conversationId;
            set => SetProperty(ref _conversationId, value);
        }

        public string SenderId
        {
            get => _senderId;
            set => SetProperty(ref _senderId, value);
        }

        public string SenderName
        {
            get => _senderName;
            set => SetProperty(ref _senderName, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string EncryptedContent
        {
            get => _encryptedContent;
            set => SetProperty(ref _encryptedContent, value);
        }

        public string MessageType
        {
            get => _messageType;
            set => SetProperty(ref _messageType, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        public bool IsSentByCurrentUser
        {
            get => _isSentByCurrentUser;
            set => SetProperty(ref _isSentByCurrentUser, value);
        }

        public string? ReplyToId
        {
            get => _replyToId;
            set => SetProperty(ref _replyToId, value);
        }

        public string? FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string? FileUrl
        {
            get => _fileUrl;
            set => SetProperty(ref _fileUrl, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public User User { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // WebSocket message models
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? ConversationId { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class MessageSentEventArgs : EventArgs
    {
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public string? ReplyToId { get; set; }
        public string? FileName { get; set; }
        public byte[]? FileData { get; set; }
    }

    public class ConversationSelectedEventArgs : EventArgs
    {
        public string ConversationId { get; set; } = string.Empty;
        public Conversation? Conversation { get; set; }
    }
}