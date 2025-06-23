using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrLab.Desktop.Models.Messaging
{
    public class MessageModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _conversationId = string.Empty;
        private string _senderId = string.Empty;
        private string _senderName = string.Empty;
        private string _content = string.Empty;
        private string _encryptedContent = string.Empty;
        private string _messageType = "text";
        private DateTime _timestamp;
        private bool _isRead;
        private bool _isDecrypted;
        private bool _isSentByCurrentUser;
        private string? _replyToId;
        private MessageModel? _replyToMessage;
        private bool _showSenderName;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string ConversationId
        {
            get => _conversationId;
            set { _conversationId = value; OnPropertyChanged(); }
        }

        public string SenderId
        {
            get => _senderId;
            set
            {
                _senderId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSentByCurrentUser));
            }
        }

        public string SenderName
        {
            get => _senderName;
            set { _senderName = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public string EncryptedContent
        {
            get => _encryptedContent;
            set { _encryptedContent = value; OnPropertyChanged(); }
        }

        public string MessageType
        {
            get => _messageType;
            set { _messageType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFileMessage)); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampFormatted)); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); }
        }

        public bool IsDecrypted
        {
            get => _isDecrypted;
            set { _isDecrypted = value; OnPropertyChanged(); }
        }

        public bool IsSentByCurrentUser
        {
            get => _isSentByCurrentUser;
            set { _isSentByCurrentUser = value; OnPropertyChanged(); }
        }

        public string? ReplyToId
        {
            get => _replyToId;
            set { _replyToId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsReply)); }
        }

        public MessageModel? ReplyToMessage
        {
            get => _replyToMessage;
            set { _replyToMessage = value; OnPropertyChanged(); }
        }

        public bool ShowSenderName
        {
            get => _showSenderName;
            set { _showSenderName = value; OnPropertyChanged(); }
        }

        // Computed properties
        public bool IsReply => !string.IsNullOrEmpty(ReplyToId);
        public bool IsFileMessage => MessageType == "file";

        public string TimestampFormatted
        {
            get
            {
                var now = DateTime.Now;
                var diff = now - Timestamp;

                if (diff.TotalMinutes < 1)
                    return "just now";
                if (diff.TotalHours < 1)
                    return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalDays < 1)
                    return Timestamp.ToString("HH:mm");
                if (diff.TotalDays < 7)
                    return Timestamp.ToString("ddd HH:mm");

                return Timestamp.ToString("MMM dd, HH:mm");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}