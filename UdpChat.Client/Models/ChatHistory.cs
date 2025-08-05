using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace UdpChat.Client.Models
{
    /// <summary>
    /// Запись в истории чата
    /// </summary>
    public class ChatHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsDelivered { get; set; }
        public string MessageId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Модель истории чата
    /// </summary>
    public class ChatHistory
    {
        public List<ChatHistoryEntry> Messages { get; set; } = new List<ChatHistoryEntry>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Добавляет новое сообщение в историю
        /// </summary>
        public void AddMessage(string senderId, string senderName, string message, string messageId, bool isDelivered = false)
        {
            var entry = new ChatHistoryEntry
            {
                Timestamp = DateTime.Now,
                SenderId = senderId,
                SenderName = senderName,
                Message = message,
                MessageId = messageId,
                IsDelivered = isDelivered
            };

            Messages.Add(entry);
        }

        /// <summary>
        /// Отмечает сообщение как доставленное
        /// </summary>
        public void MarkAsDelivered(string messageId)
        {
            var message = Messages.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                message.IsDelivered = true;
            }
        }

        /// <summary>
        /// Сохраняет историю в файл
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения истории чата: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает историю из файла
        /// </summary>
        public static ChatHistory LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new ChatHistory();
                }

                var json = File.ReadAllText(filePath);
                var history = JsonSerializer.Deserialize<ChatHistory>(json);
                return history ?? new ChatHistory();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки истории чата: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает старые сообщения (старше указанного количества дней)
        /// </summary>
        public void CleanupOldMessages(int daysToKeep = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            Messages.RemoveAll(m => m.Timestamp < cutoffDate);
        }
    }
} 