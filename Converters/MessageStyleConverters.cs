using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace DrLab.Desktop.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isSentByCurrentUser)
            {
                if (isSentByCurrentUser)
                {
                    // Sent message - blue background
                    return new SolidColorBrush(Color.FromArgb(255, 33, 150, 243));
                }
                else
                {
                    // Received message - light gray background
                    return new SolidColorBrush(Color.FromArgb(255, 241, 243, 244));
                }
            }
            return new SolidColorBrush(Color.FromArgb(255, 241, 243, 244));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isSentByCurrentUser)
            {
                if (isSentByCurrentUser)
                {
                    // Sent message - white text
                    return new SolidColorBrush(Colors.White);
                }
                else
                {
                    // Received message - dark text
                    return new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
                }
            }
            return new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isSentByCurrentUser)
            {
                return isSentByCurrentUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var diff = now - dateTime;

                if (diff.TotalMinutes < 1)
                    return "just now";
                if (diff.TotalHours < 1)
                    return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalDays < 1)
                    return dateTime.ToString("HH:mm");
                if (diff.TotalDays < 7)
                    return dateTime.ToString("ddd HH:mm");

                return dateTime.ToString("MMM dd, HH:mm");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class UnreadCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class OnlineStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isOnline)
            {
                return isOnline ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}