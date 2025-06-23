using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DrLab.Desktop;

namespace LIMS.Models.Messaging
{
    public class ConversationModel : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _conversationType;
        private DateTime _lastMessageTime;
        private string _lastMessagePreview;
        private int _unreadCount;
        private bool _isActive;
        private MessageModel _lastMessage;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string ConversationType
        {
            get => _conversationType;
            set { _conversationType = value; OnPropertyChanged(); }
        }

        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set { _lastMessageTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastMessageTimeFormatted)); }
        }

        public string LastMessagePreview
        {
            get => _lastMessagePreview;
            set { _lastMessagePreview = value; OnPropertyChanged(); }
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnreadMessages)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public MessageModel LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<UserModel> Participants { get; set; } = new ObservableCollection<UserModel>();
        public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();

        // Computed properties
        public bool HasUnreadMessages => UnreadCount > 0;

        public string LastMessageTimeFormatted
        {
            get
            {
                if (LastMessageTime == default) return "";

                var now = DateTime.Now;
                var diff = now - LastMessageTime;

                if (diff.TotalMinutes < 1) return "Now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
                if (diff.TotalDays < 7) return LastMessageTime.ToString("ddd");
                return LastMessageTime.ToString("dd/MM");
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Name)) return Name;

                if (ConversationType == "direct" && Participants.Count == 2)
                {
                    // For direct messages, show other participant's name
                    foreach (var participant in Participants)
                    {
                        if (participant.Id != App.CurrentUserId) // Assuming we have current user ID
                        {
                            return participant.DisplayName;
                        }
                    }
                }

                return "Unknown Conversation";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MessageModel : INotifyPropertyChanged
    {
        private string _id;
        private string _conversationId;
        private string _senderId;
        private string _senderName;
        private string _content;
        private string _encryptedContent;
        private string _messageType;
        private DateTime _timestamp;
        private bool _isRead;
        private bool _isSentByCurrentUser;
        private MessageModel _replyTo;
        private bool _isDecrypted;

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
            set { _senderId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSentByCurrentUser)); }
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
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampFormatted)); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); }
        }

        public bool IsSentByCurrentUser
        {
            get => _isSentByCurrentUser || SenderId == App.CurrentUserId;
            set { _isSentByCurrentUser = value; OnPropertyChanged(); }
        }

        public MessageModel ReplyTo
        {
            get => _replyTo;
            set { _replyTo = value; OnPropertyChanged(); }
        }

        public bool IsDecrypted
        {
            get => _isDecrypted;
            set { _isDecrypted = value; OnPropertyChanged(); }
        }

        public string TimestampFormatted
        {
            get
            {
                var now = DateTime.Now;
                if (Timestamp.Date == now.Date)
                {
                    return Timestamp.ToString("HH:mm");
                }
                else if (Timestamp.Date == now.Date.AddDays(-1))
                {
                    return "Yesterday " + Timestamp.ToString("HH:mm");
                }
                else
                {
                    return Timestamp.ToString("dd/MM/yyyy HH:mm");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UserModel : INotifyPropertyChanged
    {
        private string _id;
        private string _username;
        private string _firstName;
        private string _lastName;
        private string _email;
        private bool _isOnline;
        private DateTime _lastSeen;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set { _lastSeen = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
                {
                    return $"{FirstName} {LastName}".Trim();
                }
                return Username ?? "Unknown User";
            }
        }

        public string Initials
        {
            get
            {
                var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                }
                else if (parts.Length == 1 && parts[0].Length > 0)
                {
                    return parts[0][0].ToString().ToUpper();
                }
                return "?";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}