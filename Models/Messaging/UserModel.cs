using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrLab.Desktop.Models.Messaging
{
    public class UserModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _department = string.Empty;
        private bool _isOnline;
        private DateTime? _lastSeen;
        private string _avatarUrl = string.Empty;

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

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
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

        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(); }
        }

        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public DateTime? LastSeen
        {
            get => _lastSeen;
            set { _lastSeen = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public string AvatarUrl
        {
            get => _avatarUrl;
            set { _avatarUrl = value; OnPropertyChanged(); }
        }

        // Computed properties
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
                {
                    return $"{FirstName} {LastName}".Trim();
                }
                return Username;
            }
        }

        public string StatusText
        {
            get
            {
                if (IsOnline) return "Online";
                if (LastSeen.HasValue)
                {
                    var diff = DateTime.Now - LastSeen.Value;
                    if (diff.TotalMinutes < 5) return "Just now";
                    if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                    if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                    return $"{(int)diff.TotalDays}d ago";
                }
                return "Offline";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}