using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LIMS.Models.Messaging
{
    public class NotificationModel : INotifyPropertyChanged
    {
        private string _id;
        private string _title;
        private string _message;
        private DateTime _timestamp;
        private bool _isRead;
        private string _conversationId;
        private NotificationType _type;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); }
        }

        public string ConversationId
        {
            get => _conversationId;
            set { _conversationId = value; OnPropertyChanged(); }
        }

        public NotificationType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum NotificationType
    {
        NewMessage,
        ConversationInvite,
        SystemAlert
    }
}