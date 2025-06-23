using System;
using System.Windows;
using System.Windows.Controls;
using DrLab.Desktop.Views.Messaging;
using LIMS.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LIMS.Views
{
    public partial class MessagingPage : Page
    {
        public MessagingViewModel ViewModel { get; private set; }

        public MessagingPage()
        {
            InitializeComponent();
            ViewModel = new MessagingViewModel();
            DataContext = ViewModel;

            // Wire up events
            ConversationListPanel.ConversationSelected += OnConversationSelected;
            MessageView.MessageSent += OnMessageSent;

            Loaded += MessagingPage_Loaded;
            Unloaded += MessagingPage_Unloaded;
        }

        private async void MessagingPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }

        private async void MessagingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.CleanupAsync();
        }

        private void OnConversationSelected(object sender, ConversationSelectedEventArgs e)
        {
            ViewModel.SelectConversation(e.ConversationId);
        }

        private void OnMessageSent(object sender, MessageSentEventArgs e)
        {
            ViewModel.SendMessage(e.Content, e.MessageType);
        }
    }

    // Event Args
    public class ConversationSelectedEventArgs : EventArgs
    {
        public string ConversationId { get; set; }
    }

    public class MessageSentEventArgs : EventArgs
    {
        public string Content { get; set; }
        public string MessageType { get; set; } = "text";
    }
}