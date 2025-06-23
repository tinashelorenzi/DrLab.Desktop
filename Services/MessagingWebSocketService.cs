using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace DrLab.Desktop.Services
{
    public class MessagingWebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly DispatcherQueue _dispatcher;
        private bool _isConnected;

        // Events
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConversationUpdated;
        public event EventHandler<string>? NotificationReceived;
        public event EventHandler<string>? ReadStatusUpdated;
        public event EventHandler<string>? TypingIndicatorReceived;

        public bool IsConnected => _isConnected;

        public MessagingWebSocketService()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
        }

        public async Task ConnectAsync(string authToken)
        {
            try
            {
                _webSocket?.Dispose();
                _cancellationTokenSource?.Dispose();

                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Add authorization header
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");

                // Connect to WebSocket endpoint
                var uri = new Uri("ws://localhost:8000/ws/messaging/");
                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, true);

                // Start listening for messages
                _ = Task.Run(async () => await ListenForMessagesAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                System.Diagnostics.Debug.WriteLine($"WebSocket connection failed: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during WebSocket disconnect: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        public async Task SendMessageAsync(object message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not connected");

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessIncomingMessage(json);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in WebSocket listening: {ex.Message}");
                _isConnected = false;
                _dispatcher.TryEnqueue(() => ConnectionStatusChanged?.Invoke(this, false));
            }
        }

        private async Task ProcessIncomingMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<WebSocketMessage>(json);
                if (message?.Type == null) return;

                // Dispatch to UI thread
                _dispatcher.TryEnqueue(() =>
                {
                    switch (message.Type)
                    {
                        case "message":
                            MessageReceived?.Invoke(this, json);
                            break;
                        case "conversation_updated":
                            ConversationUpdated?.Invoke(this, json);
                            break;
                        case "notification":
                            NotificationReceived?.Invoke(this, json);
                            break;
                        case "read_status":
                            ReadStatusUpdated?.Invoke(this, json);
                            break;
                        case "typing":
                            TypingIndicatorReceived?.Invoke(this, json);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing WebSocket message: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }

    public class WebSocketMessage
    {
        public string? Type { get; set; }
        public object? Data { get; set; }
    }
}