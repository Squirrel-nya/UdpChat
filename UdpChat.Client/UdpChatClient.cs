using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpChat.Client.Models;
using Timer = System.Threading.Timer;

namespace UdpChat.Client
{

    public class UdpChatClient : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly Timer _pingTimer;
        private readonly Timer _retryTimer;
        private readonly Dictionary<string, DateTime> _pendingMessages;
        private readonly Dictionary<string,string> _nicknameToId = new();
        private readonly HashSet<string> _blackList = new();
        private readonly HashSet<string> _myRooms = new();
        private readonly ChatHistory _chatHistory;
        private readonly string _historyFilePath;

        public string ClientId { get; private set; }
        public string Nickname { get; private set; } = string.Empty;
        public string ServerAddress { get; private set; } = "127.0.0.1";
        public int ServerPort { get; private set; } = 9000;
        public bool IsConnected { get; private set; }

        // События для уведомления UI
        public event EventHandler<string>? MessageReceived;
        public event EventHandler<List<string>>? UserListUpdated;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? MessageDelivered;

        public UdpChatClient(string nickname, string serverAddress = "127.0.0.1", int serverPort = 9000)
        {
            Nickname = nickname;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            ClientId = Guid.NewGuid().ToString();

            _udpClient = new UdpClient();
            _pendingMessages = new Dictionary<string, DateTime>();
            _historyFilePath = $"chat_history_{ClientId}.json";
            
            // Загружаем историю чата
            _chatHistory = ChatHistory.LoadFromFile(_historyFilePath);
            _chatHistory.ClientId = ClientId;

            // Таймер для отправки PING каждые 30 секунд
            _pingTimer = new Timer(SendPing, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            
            // Таймер для повторной отправки неподтвержденных сообщений
            _retryTimer = new Timer(RetryPendingMessages, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

         /// Подключается к серверу
        public async Task ConnectAsync()
        {
            try
            {
                // Отправляем HELLO сообщение
                var helloMessage = ChatMessage.CreateHello(ClientId, Nickname);
                await SendMessageAsync(helloMessage);

                IsConnected = true;
                OnStatusChanged("Подключен к серверу");

                // Запускаем прослушивание сообщений
                _ = Task.Run(ListenForMessagesAsync);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Ошибка подключения: {ex.Message}");
                throw;
            }
        }

        /// Отключается от сервера
        public void Disconnect()
        {
            IsConnected = false;
            _pingTimer?.Dispose();
            _retryTimer?.Dispose();
            _udpClient?.Close();
            OnStatusChanged("Отключен от сервера");
        }

         /// Отправляет сообщение всем пользователям
       public async Task SendMessageToAllAsync(string message)
        {
            var chatMessage = ChatMessage.CreateMessage(ClientId, "ALL", message);
            await SendMessageAsync(chatMessage);
            
            // Добавляем в историю
            _chatHistory.AddMessage(ClientId, Nickname, message, chatMessage.MessageId);
            _pendingMessages[chatMessage.MessageId] = DateTime.Now;
        }

          /// Отправляет сообщение конкретному пользователю
        public async Task SendMessageToUserAsync(string destinationId, string message)
        {
            var chatMessage = ChatMessage.CreateMessage(ClientId, destinationId, message);
            await SendMessageAsync(chatMessage);
            
            // Добавляем в историю
            _chatHistory.AddMessage(ClientId, Nickname, message, chatMessage.MessageId);
            _pendingMessages[chatMessage.MessageId] = DateTime.Now;
        }

        //Группы
        public async Task JoinRoomAsync(string room)
        {
            if (_myRooms.Contains(room)) return;
            _myRooms.Add(room);
            await SendMessageAsync(ChatMessage.CreateMessage(ClientId, "SERVER", $"JOIN:{room}"));
        }

        public async Task LeaveRoomAsync(string room)
        {
            if (!_myRooms.Contains(room)) return;
            _myRooms.Remove(room);
            await SendMessageAsync(ChatMessage.CreateMessage(ClientId, "SERVER", $"LEAVE:{room}"));
        }

        public async Task SendMessageToRoomAsync(string room, string text)
        {
            if (!_myRooms.Contains(room))
            {
                OnStatusChanged($"Вы не состоите в комнате {room}");
                return;
            }
            await SendMessageAsync(ChatMessage.CreateMessage(ClientId, "SERVER", $"GROUP:{room}:{text}"));
        }

        // Чёрный список
        public void BlockUser(string userId)   => _blackList.Add(userId);
        public void UnblockUser(string userId) => _blackList.Remove(userId);

        // helper
        public bool IsBlocked(string id) => _blackList.Contains(id);

        public string? GetIdByNickname(string nickname)
        {
            return _nicknameToId.TryGetValue(nickname, out var id) ? id : null;
        }

        /// Отправляет сообщение на сервер
        private async Task SendMessageAsync(ChatMessage message)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message.ToString());
                var serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerAddress), ServerPort);
                await _udpClient.SendAsync(data, data.Length, serverEndPoint);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Ошибка отправки сообщения: {ex.Message}");
                throw;
            }
        }

        /// Прослушивает входящие сообщения
        private async Task ListenForMessagesAsync()
        {
            try
            {
                while (IsConnected)
                {
                    var result = await _udpClient.ReceiveAsync();
                    await ProcessIncomingMessageAsync(result.Buffer);
                }
            }
            catch (Exception ex)
            {
                if (IsConnected)
                {
                    OnStatusChanged($"Ошибка получения сообщений: {ex.Message}");
                }
            }
        }

        /// Обрабатывает входящее сообщение
        private async Task ProcessIncomingMessageAsync(byte[] data)
        {
            try
            {
                var messageText = Encoding.UTF8.GetString(data);
                var message = ChatMessage.Parse(messageText);

                switch (message.Header)
                {
                    case MessageType.ACK:
                        HandleAckMessage(message);
                        break;
                    case MessageType.MSG:
                        await HandleChatMessageAsync(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        /// Обрабатывает ACK сообщение
        private void HandleAckMessage(ChatMessage message)
        {
            if (_pendingMessages.ContainsKey(message.MessageId))
            {
                _pendingMessages.Remove(message.MessageId);
                _chatHistory.MarkAsDelivered(message.MessageId);
                OnMessageDelivered(message.MessageId);
            }
        }

        /// Обрабатывает текстовое сообщение
        private async Task HandleChatMessageAsync(ChatMessage message)
        {
            // чёрный список
            if (_blackList.Contains(message.SourceId)) return;
            {
                // Игнорируем собственные сообщения
                if (message.SourceId == ClientId)
                    return;

                // Проверяем, является ли это сообщением со списком пользователей
                if (message.Body.StartsWith("USERS:"))
                {
                    HandleUserListMessage(message.Body);
                    return;
                }

                // Добавляем сообщение в историю（групповые и личные）
                _chatHistory.AddMessage(message.SourceId, message.SourceId, message.Body, message.MessageId, true);

                // Отправляем ACK
                var ackMessage = ChatMessage.CreateAck(message.MessageId, ClientId, message.SourceId);
                await SendMessageAsync(ackMessage);

                // Уведомляем UI о новом сообщении
                OnMessageReceived($"[{DateTime.Now:HH:mm:ss}] {message.SourceId}: {message.Body}");
            }
        }

        /// Обрабатывает сообщение со списком пользователей
        private void HandleUserListMessage(string userListData)
        {
            _nicknameToId.Clear();
            var users = userListData.Replace("USERS:", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var userList = new List<string>();
            foreach (var u in users)
            {
                var parts = u.Split(':');
                if (parts.Length != 2) continue;
                var nick = parts[0];
                var id   = parts[1];
                _nicknameToId[nick] = id;
                userList.Add(nick);
            }
            OnUserListUpdated(userList);
        }

        /// Отправляет PING сообщение
        private async void SendPing(object? state)
        {
            if (IsConnected)
            {
                try
                {
                    var pingMessage = ChatMessage.CreatePing(ClientId);
                    await SendMessageAsync(pingMessage);
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Ошибка отправки PING: {ex.Message}");
                }
            }
        }

        /// Повторно отправляет неподтвержденные сообщения
        private async void RetryPendingMessages(object? state)
        {
            if (!IsConnected) return;

            var messagesToRetry = _pendingMessages
                .Where(kvp => DateTime.Now.Subtract(kvp.Value).TotalSeconds > 10)
                .ToList();

            foreach (var kvp in messagesToRetry)
            {
                try
                {
                    _pendingMessages.Remove(kvp.Key);
                    OnStatusChanged($"Сообщение {kvp.Key} не было доставлено");
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Ошибка повторной отправки: {ex.Message}");
                }
            }
        }

        /// Сохраняет историю чата
        public void SaveChatHistory()
        {
            try
            {
                _chatHistory.SaveToFile(_historyFilePath);
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Ошибка сохранения истории: {ex.Message}");
            }
        }

        /// Получает историю чата
        public List<ChatHistoryEntry> GetChatHistory()
        {
            return _chatHistory.Messages.ToList();
        }

        protected virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        protected virtual void OnUserListUpdated(List<string> users)
        {
            UserListUpdated?.Invoke(this, users);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        protected virtual void OnMessageDelivered(string messageId)
        {
            MessageDelivered?.Invoke(this, messageId);
        }

        public void Dispose()
        {
            Disconnect();
            _udpClient?.Dispose();
            _pingTimer?.Dispose();
            _retryTimer?.Dispose();
        }
    }
} 