// Services/MessagingWebSocketService.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace DrLab.Desktop.Services
{
    public class MessagingWebSocketService : INotifyPropertyChanged, IDisposable
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _baseUrl;
        private string? _authToken;
        private bool _isConnected;
        private readonly object _lockObject = new object();
        private readonly DispatcherQueue _dispatcher;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<MessageReceivedEventArgs>? MessageReceived;
        public event Action<ConversationUpdateEventArgs>? ConversationUpdated;
        public event Action<NotificationEventArgs>? NotificationReceived;
        public event Action<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        public event Action<ReadStatusUpdateEventArgs>? ReadStatusUpdated;
        public event Action<TypingIndicatorEventArgs>? TypingIndicatorReceived;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    ConnectionStatusChanged?.Invoke(new ConnectionStatusEventArgs { IsConnected = value });
                }
            }
        }

        public MessagingWebSocketService(string baseUrl)
        {
            _baseUrl = baseUrl.Replace("http", "ws").Replace("https", "wss");
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public async Task ConnectAsync(string authToken)
        {
            try
            {
                if (IsConnected)
                {
                    await DisconnectAsync();
                }

                _authToken = authToken;
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                // Add authorization header
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

                // Connect to WebSocket endpoint
                var uri = new Uri($"{_baseUrl}/ws/messaging/");
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                IsConnected = true;

                // Start listening for messages
                _ = Task.Run(ListenForMessages);

                // Send connection confirmation
                await SendAsync(new { type = "connection_established" });
            }
            catch (Exception ex)
            {
                IsConnected = false;
                throw new Exception($"Failed to connect to messaging server: {ex.Message}", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                IsConnected = false;
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _cancellationTokenSource?.Dispose();
                _webSocket = null;
                _cancellationTokenSource = null;
            }
        }

        public async Task SendMessageAsync(string conversationId, string encryptedContent, string messageType = "text", string? replyToId = null)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "send_message",
                conversation_id = conversationId,
                encrypted_content = encryptedContent,
                message_type = messageType,
                reply_to_id = replyToId
            });
        }

        public async Task JoinConversationAsync(string conversationId)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "join_conversation",
                conversation_id = conversationId
            });
        }

        public async Task LeaveConversationAsync(string conversationId)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "leave_conversation",
                conversation_id = conversationId
            });
        }

        public async Task MarkAsReadAsync(string messageId)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "mark_as_read",
                message_id = messageId
            });
        }

        public async Task SendTypingIndicatorAsync(string conversationId, bool isTyping)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to messaging server");

            await SendAsync(new
            {
                type = "typing_indicator",
                conversation_id = conversationId,
                is_typing = isTyping
            });
        }

        private async Task SendAsync(object data)
        {
            try
            {
                if (_webSocket?.State != WebSocketState.Open)
                    throw new InvalidOperationException("WebSocket is not open");

                var json = JsonSerializer.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                var arraySegment = new ArraySegment<byte>(bytes);

                await _webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send message: {ex.Message}", ex);
            }
        }

        private async Task ListenForMessages()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource?.Token ?? CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleReceivedMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        IsConnected = false;
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in message listener: {ex.Message}");
                IsConnected = false;
            }
        }

        private async Task HandleReceivedMessage(string message)
        {
            try
            {
                var root = JsonDocument.Parse(message).RootElement;
                var messageType = root.GetProperty("type").GetString();

                switch (messageType)
                {
                    case "new_message":
                        await HandleNewMessage(root);
                        break;

                    case "conversation_updated":
                        await HandleConversationUpdate(root);
                        break;

                    case "new_message_notification":
                        await HandleNotification(root);
                        break;

                    case "read_status_update":
                        await HandleReadStatusUpdate(root);
                        break;

                    case "typing_indicator":
                        await HandleTypingIndicator(root);
                        break;

                    case "connection_established":
                        System.Diagnostics.Debug.WriteLine("WebSocket connection established");
                        break;

                    case "error":
                        var errorMessage = root.GetProperty("message").GetString();
                        System.Diagnostics.Debug.WriteLine($"WebSocket error: {errorMessage}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling received message: {ex.Message}");
            }
        }

        private async Task HandleNewMessage(JsonElement root)
        {
            var messageData = new MessageReceivedEventArgs
            {
                MessageId = root.GetProperty("message_id").GetString() ?? string.Empty,
                ConversationId = root.GetProperty("conversation_id").GetString() ?? string.Empty,
                SenderId = root.GetProperty("sender_id").GetString() ?? string.Empty,
                SenderName = root.GetProperty("sender_name").GetString() ?? string.Empty,
                EncryptedContent = root.GetProperty("encrypted_content").GetString() ?? string.Empty,
                MessageType = root.GetProperty("message_type").GetString() ?? "text",
                Timestamp = DateTime.Parse(root.GetProperty("timestamp").GetString() ?? DateTime.Now.ToString()),
                ReplyToId = root.TryGetProperty("reply_to_id", out var replyTo) ? replyTo.GetString() : null
            };

            _dispatcher.TryEnqueue(() => MessageReceived?.Invoke(messageData));
        }

        private async Task HandleConversationUpdate(JsonElement root)
        {
            var updateData = new ConversationUpdateEventArgs
            {
                ConversationId = root.GetProperty("conversation_id").GetString() ?? string.Empty,
                UpdateType = root.GetProperty("update_type").GetString() ?? string.Empty,
                Data = root.TryGetProperty("data", out var data) ? data.GetRawText() : string.Empty
            };

            _dispatcher.TryEnqueue(() => ConversationUpdated?.Invoke(updateData));
        }

        private async Task HandleNotification(JsonElement root)
        {
            var notificationData = new NotificationEventArgs
            {
                ConversationId = root.GetProperty("conversation_id").GetString() ?? string.Empty,
                ConversationName = root.GetProperty("conversation_name").GetString() ?? string.Empty,
                SenderName = root.GetProperty("sender_name").GetString() ?? string.Empty,
                MessagePreview = root.GetProperty("message_preview").GetString() ?? string.Empty,
                Timestamp = DateTime.Parse(root.GetProperty("timestamp").GetString() ?? DateTime.Now.ToString())
            };

            _dispatcher.TryEnqueue(() => NotificationReceived?.Invoke(notificationData));
        }

        private async Task HandleReadStatusUpdate(JsonElement root)
        {
            var readStatusData = new ReadStatusUpdateEventArgs
            {
                MessageId = root.GetProperty("message_id").GetString() ?? string.Empty,
                UserId = root.GetProperty("user_id").GetString() ?? string.Empty,
                ConversationId = root.GetProperty("conversation_id").GetString() ?? string.Empty,
                ReadAt = DateTime.Parse(root.GetProperty("read_at").GetString() ?? DateTime.Now.ToString())
            };

            _dispatcher.TryEnqueue(() => ReadStatusUpdated?.Invoke(readStatusData));
        }

        private async Task HandleTypingIndicator(JsonElement root)
        {
            var typingData = new TypingIndicatorEventArgs
            {
                ConversationId = root.GetProperty("conversation_id").GetString() ?? string.Empty,
                UserId = root.GetProperty("user_id").GetString() ?? string.Empty,
                UserName = root.GetProperty("user_name").GetString() ?? string.Empty,
                IsTyping = root.GetProperty("is_typing").GetBoolean()
            };

            _dispatcher.TryEnqueue(() => TypingIndicatorReceived?.Invoke(typingData));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            DisconnectAsync().Wait(5000); // Wait up to 5 seconds for graceful disconnect
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }

    // Event argument classes
    public class MessageReceivedEventArgs
    {
        public string MessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string EncryptedContent { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public DateTime Timestamp { get; set; }
        public string? ReplyToId { get; set; }
    }

    public class ConversationUpdateEventArgs
    {
        public string ConversationId { get; set; } = string.Empty;
        public string UpdateType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    public class NotificationEventArgs
    {
        public string ConversationId { get; set; } = string.Empty;
        public string ConversationName { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string MessagePreview { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ConnectionStatusEventArgs
    {
        public bool IsConnected { get; set; }
    }

    public class ReadStatusUpdateEventArgs
    {
        public string MessageId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; }
    }

    public class TypingIndicatorEventArgs
    {
        public string ConversationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsTyping { get; set; }
    }
}