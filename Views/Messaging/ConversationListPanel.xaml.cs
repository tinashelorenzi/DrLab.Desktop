using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LIMS.Models.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LIMS.Views.Messaging
{
    public partial class ConversationListPanel : UserControl
    {
        public event EventHandler<ConversationSelectedEventArgs> ConversationSelected;

        public ConversationListPanel()
        {
            InitializeComponent();

            // Wire up events
            ConversationsList.MouseDoubleClick += ConversationsList_MouseDoubleClick;
            ConversationsList.SelectionChanged += ConversationsList_SelectionChanged;
            NewMessageButton.Click += NewMessageButton_Click;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
        }

        private void ConversationsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConversationsList.SelectedItem is ConversationModel conversation)
            {
                OnConversationSelected(conversation.Id);
            }
        }

        private void ConversationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ConversationModel conversation)
            {
                OnConversationSelected(conversation.Id);
            }
        }

        private void NewMessageButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open new message dialog
            MessageBox.Show("New message dialog would open here", "New Message",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Implement conversation search/filtering
            var searchText = SearchTextBox.Text.ToLower();
            // Filter conversations based on search text
        }

        protected virtual void OnConversationSelected(string conversationId)
        {
            ConversationSelected?.Invoke(this, new ConversationSelectedEventArgs { ConversationId = conversationId });
        }
    }
}