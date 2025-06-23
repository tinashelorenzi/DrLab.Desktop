using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LIMS.Views.Messaging
{
    public partial class MessageView : UserControl
    {
        public event EventHandler<MessageSentEventArgs> MessageSent;

        public MessageView()
        {
            InitializeComponent();

            // Wire up events
            SendButton.Click += SendButton_Click;
            MessageTextBox.KeyDown += MessageTextBox_KeyDown;
            AttachmentButton.Click += AttachmentButton_Click;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void AttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open file picker dialog
            MessageBox.Show("File attachment dialog would open here", "Attach File",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendMessage()
        {
            var content = MessageTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(content))
            {
                OnMessageSent(content);
                MessageTextBox.Text = string.Empty;
            }
        }

        protected virtual void OnMessageSent(string content, string messageType = "text")
        {
            MessageSent?.Invoke(this, new MessageSentEventArgs { Content = content, MessageType = messageType });
        }
    }
}