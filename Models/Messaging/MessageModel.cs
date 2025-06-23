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
        private DateTime _timestamp = DateTime.Now;
        private bool _isRead;
        private bool _isSentByCurrentUser;
        private MessageModel? _replyTo;
        private bool _isDecrypted;
        private string? _replyToId;

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
                // Update IsSentByCurrentUser when SenderId changes
                IsSentByCurrentUser = _senderId == App.CurrentUserId;
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
            set { _messageType = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeFormatted)); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); }
        }

        public bool IsSentByCurrentUser
        {
            get => _isSentByCurrentUser;
            set { _isSentByCurrentUser = value; OnPropertyChanged(); }
        }

        public MessageModel? ReplyTo
        {
            get => _replyTo;
            set { _replyTo = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsReply)); }
        }

        public string? ReplyToId
        {
            get => _replyToId;
            set { _replyToId = value; OnPropertyChanged(); }
        }

        public bool IsDecrypted
        {
            get => _isDecrypted;
            set { _isDecrypted = value; OnPropertyChanged(); }
        }

        // Computed properties
        public bool IsReply => ReplyTo != null || !string.IsNullOrEmpty(ReplyToId);

        public string TimeFormatted
        {
            get
            {
                var now = DateTime.Now;
                var diff = now - Timestamp;

                if (diff.TotalMinutes < 1) return "Now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return Timestamp.ToString("HH:mm");
                if (diff.TotalDays < 7) return Timestamp.ToString("ddd HH:mm");
                return Timestamp.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}