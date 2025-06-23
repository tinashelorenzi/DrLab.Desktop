using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrLab.Desktop.Models.Messaging
{
    public class ConversationModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _conversationType = "direct";
        private DateTime _lastMessageTime = DateTime.Now;
        private string _lastMessagePreview = string.Empty;
        private int _unreadCount;
        private bool _isActive;
        private MessageModel? _lastMessage;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string ConversationType
        {
            get => _conversationType;
            set { _conversationType = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
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

        public MessageModel? LastMessage
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
                        if (participant.Id != App.CurrentUserId)
                        {
                            return participant.DisplayName;
                        }
                    }
                }

                return "Unknown Conversation";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}