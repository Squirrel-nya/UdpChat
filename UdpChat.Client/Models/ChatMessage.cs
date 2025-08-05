using System;

namespace UdpChat.Client.Models
{
    /// <summary>
    /// Типы сообщений в чате
    /// </summary>
    public enum MessageType
    {
        HELLO,  // Приветствие при подключении
        PING,   // Проверка активности
        MSG,    // Текстовое сообщение
        ACK     // Подтверждение получения
    }

    /// <summary>
    /// Модель сообщения чата
    /// </summary>
    public class ChatMessage
    {
        public MessageType Header { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string DestinationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Создает сообщение из строки формата HEADER|SRC_ID|DST_ID|MSG_ID|BODY
        /// </summary>
        public static ChatMessage Parse(string message)
        {
            var parts = message.Split('|');
            if (parts.Length < 5)
                throw new ArgumentException("Неверный формат сообщения");

            return new ChatMessage
            {
                Header = Enum.Parse<MessageType>(parts[0]),
                SourceId = parts[1],
                DestinationId = parts[2],
                MessageId = parts[3],
                Body = parts[4]
            };
        }

        /// <summary>
        /// Преобразует сообщение в строку для отправки
        /// </summary>
        public override string ToString()
        {
            return $"{Header}|{SourceId}|{DestinationId}|{MessageId}|{Body}";
        }

        /// <summary>
        /// Создает ACK сообщение для подтверждения получения
        /// </summary>
        public static ChatMessage CreateAck(string originalMessageId, string sourceId, string destinationId)
        {
            return new ChatMessage
            {
                Header = MessageType.ACK,
                SourceId = destinationId,  // ACK отправляется от получателя
                DestinationId = sourceId,  // ACK отправляется отправителю
                MessageId = originalMessageId,
                Body = "OK"
            };
        }

        /// <summary>
        /// Создает HELLO сообщение для регистрации клиента
        /// </summary>
        public static ChatMessage CreateHello(string clientId, string nickname)
        {
            return new ChatMessage
            {
                Header = MessageType.HELLO,
                SourceId = clientId,
                DestinationId = "SERVER",
                MessageId = Guid.NewGuid().ToString(),
                Body = nickname
            };
        }

        /// <summary>
        /// Создает PING сообщение для проверки активности
        /// </summary>
        public static ChatMessage CreatePing(string clientId)
        {
            return new ChatMessage
            {
                Header = MessageType.PING,
                SourceId = clientId,
                DestinationId = "SERVER",
                MessageId = Guid.NewGuid().ToString(),
                Body = "PING"
            };
        }

        /// <summary>
        /// Создает MSG сообщение для отправки текста
        /// </summary>
        public static ChatMessage CreateMessage(string sourceId, string destinationId, string text)
        {
            return new ChatMessage
            {
                Header = MessageType.MSG,
                SourceId = sourceId,
                DestinationId = destinationId,
                MessageId = Guid.NewGuid().ToString(),
                Body = text
            };
        }
    }
} 