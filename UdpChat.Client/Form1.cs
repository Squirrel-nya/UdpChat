using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UdpChat.Client
{
    public partial class Form1 : Form
    {
        private UdpChatClient? _chatClient;
        private bool _isConnected = false;

        public Form1()
        {
            // InitializeComponent не требуется, интерфейс собирается вручную
            // Метод оставлен пустым, чтобы устранить ошибку компиляции
            InitializeComponent();
            InitializeChatForm();
        }

        private void InitializeChatForm()
        {
            // Настройка формы
            this.Text = "UDP Chat Client";
            this.Size = new Size(800, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Создание элементов управления
            CreateControls();

            // Настройка обработчиков событий
            SetupEventHandlers();

            // Начальное состояние
            UpdateConnectionState(false);
        }


        private ContextMenuStrip BuildUserContextMenu()
        {
            var menu = new ContextMenuStrip();
            var miBlock = new ToolStripMenuItem("Добавить в чёрный список");
            var miUnblock = new ToolStripMenuItem("Убрать из чёрного списка");
            miBlock.Click += (s, e) => ToggleBlock(true);
            miUnblock.Click += (s, e) => ToggleBlock(false);
            menu.Items.AddRange(new ToolStripItem[] { miBlock, miUnblock });
            return menu;
        }

        private void ToggleBlock(bool block)
        {
            if (lstUsers.SelectedItem == null || _chatClient == null) return;
            var nick = lstUsers.SelectedItem.ToString()!.Replace(" (вы)", "").Replace(" (блок)", "");
            var id = _chatClient.GetIdByNickname(nick);
            if (id == null) return;
            if (block)
                _chatClient.BlockUser(id);
            else
                _chatClient.UnblockUser(id);
            UpdateUserListDisplay();
        }

        private void UpdateUserListDisplay()
        {
            if (_chatClient == null) return;
            var items = lstUsers.Items.Cast<string>().Select(s => s.Replace(" (вы)", "").Replace(" (блок)", ""));
            var updated = new List<string>();
            foreach (var nick in items)
            {
                var id = _chatClient.GetIdByNickname(nick);
                if (id == null) continue;
                var label = nick;
                if (nick == txtNickname.Text) label += " (вы)";
                if (_chatClient.IsBlocked(id)) label += " (блок)";
                updated.Add(label);
            }
            lstUsers.Items.Clear();
            lstUsers.Items.AddRange(updated.ToArray());
        }

        private void CreateControls()
        {
            // Панель подключения
            var connectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblNickname = new Label
            {
                Text = "Никнейм:",
                Location = new Point(10, 15),
                Size = new Size(70, 20)
            };

            txtNickname = new TextBox
            {
                Location = new Point(90, 12),
                Size = new Size(150, 23),
                Text = "User" + new Random().Next(1000, 9999)
            };

            var lblServer = new Label
            {
                Text = "Сервер:",
                Location = new Point(250, 15),
                Size = new Size(50, 20)
            };

            txtServerAddress = new TextBox
            {
                Location = new Point(310, 12),
                Size = new Size(120, 23),
                Text = "127.0.0.1"
            };

            var lblPort = new Label
            {
                Text = "Порт:",
                Location = new Point(440, 15),
                Size = new Size(35, 20)
            };

            txtPort = new TextBox
            {
                Location = new Point(485, 12),
                Size = new Size(60, 23),
                Text = "9000"
            };

            btnConnect = new Button
            {
                Text = "Подключиться",
                Location = new Point(555, 10),
                Size = new Size(100, 30)
            };

            btnDisconnect = new Button
            {
                Text = "Отключиться",
                Location = new Point(665, 10),
                Size = new Size(100, 30),
                Enabled = false
            };

            lblStatus = new Label
            {
                Text = "Не подключен",
                Location = new Point(10, 45),
                Size = new Size(300, 20),
                ForeColor = Color.Red
            };

            // Добавляем элементы на панель подключения
            connectionPanel.Controls.AddRange(new Control[] 
            { 
                lblNickname, txtNickname, lblServer, txtServerAddress, 
                lblPort, txtPort, btnConnect, btnDisconnect, lblStatus 
            });

            // Основная панель чата
            var chatPanel = new Panel
            {
                Location = new Point(100, 90),
                Size = new Size(200, 500),
                Dock = DockStyle.Fill
            };

            // Список пользователей
            var lblUsers = new Label
             {
                 Text = "Активные пользователи:",
                 Location = new Point(10, 90),
                 Size = new Size(150, 20)
            };

            lstUsers = new ListBox
            {
                ContextMenuStrip = BuildUserContextMenu(),
                Location = new Point(10, 110),
                Size = new Size(150, 350),
                SelectionMode = SelectionMode.One
            };

            // Окно чата
            var lblChat = new Label
            {
                Text = "Сообщения:",
                Location = new Point(180, 90),
                Size = new Size(150, 20),
                Height = 20
            };

            txtChat = new RichTextBox
            {
                Location = new Point(180, 110),
                Size = new Size(590, 350),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Панель отправки сообщений
            var messagePanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 130
            };

            var lblMessage = new Label
            {
                Text = "Сообщение:",
                Location = new Point(10, 0),
                Size = new Size(80, 20)
            };

            txtMessage = new TextBox
            {
                Location = new Point(90, 0),
                Size = new Size(680, 50),
                Multiline = true
            };

            btnSend = new Button
            {
                Text = "Отправить",
                Location = new Point(670, 60),
                Size = new Size(100, 30),
            };

            btnSendToAll = new Button
            {
                Text = "Всем",
                Location = new Point(560, 60),
                Size = new Size(100, 30),
                Visible = false
            };

            btnSendToUser = new Button
            {
                Text = "Пользователю",
                Location = new Point(670, 60),
                Size = new Size(100, 30),
                Visible = false
            };

            // Добавляем элементы на панель сообщений
            messagePanel.Controls.AddRange(new Control[] 
            { 
                lblMessage, txtMessage, btnSend, btnSendToAll, btnSendToUser 
            });

            // Добавляем элементы на основную панель чата
            chatPanel.Controls.AddRange(new Control[]
            {
                lblUsers,
                lstUsers,
                messagePanel,   
                lblChat,        
                txtChat        
            });

            // Добавляем панели на форму
            this.Controls.Add(connectionPanel);
            this.Controls.Add(chatPanel);
        }

        private void SetupEventHandlers()
        {
            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;
            btnSend.Click += BtnSend_Click;
            btnSendToAll.Click += BtnSendToAll_Click;
            btnSendToUser.Click += BtnSendToUser_Click;
            txtMessage.KeyPress += TxtMessage_KeyPress;
            lstUsers.SelectedIndexChanged += LstUsers_SelectedIndexChanged;
            this.FormClosing += Form1_FormClosing;
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNickname.Text))
            {
                MessageBox.Show("Введите никнейм!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Неверный номер порта!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnConnect.Enabled = false;
                lblStatus.Text = "Подключение...";
                lblStatus.ForeColor = Color.Orange;

                _chatClient = new UdpChatClient(txtNickname.Text, txtServerAddress.Text, port);
                
                // Подписываемся на события
                _chatClient.MessageReceived += ChatClient_MessageReceived;
                _chatClient.UserListUpdated += ChatClient_UserListUpdated;
                _chatClient.StatusChanged += ChatClient_StatusChanged;
                _chatClient.MessageDelivered += ChatClient_MessageDelivered;

                await _chatClient.ConnectAsync();
                UpdateConnectionState(true);

                // Загружаем историю чата
                LoadChatHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnect.Enabled = true;
                lblStatus.Text = "Ошибка подключения";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            DisconnectFromServer();
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            await SendMessage();
        }

        private async void BtnSendToAll_Click(object? sender, EventArgs e)
        {
            await SendMessageToAll();
        }

        private async void BtnSendToUser_Click(object? sender, EventArgs e)
        {
            await SendMessageToSelectedUser();
        }

        private async void TxtMessage_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !ModifierKeys.HasFlag(Keys.Shift))
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        private void LstUsers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateSendButtons();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            DisconnectFromServer();
        }

        private async Task SendMessage()
        {
            if (_chatClient == null || !_isConnected || string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            try
            {
                if (lstUsers.SelectedItem != null)
                {
                    await SendMessageToSelectedUser();
                }
                else
                {
                    await SendMessageToAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendMessageToAll()
        {
            if (_chatClient == null || !_isConnected)
                return;

            var message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            await _chatClient.SendMessageToAllAsync(message);
            AddMessageToChat($"[{DateTime.Now:HH:mm:ss}] Вы (всем): {message}");
            txtMessage.Clear();
        }

        private async Task SendMessageToSelectedUser()
        {
            if (_chatClient == null || !_isConnected || lstUsers.SelectedItem == null)
                return;

            var message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var selectedUser = lstUsers.SelectedItem.ToString();
            if (selectedUser != null)
            {
                await _chatClient.SendMessageToUserAsync(selectedUser, message);
                AddMessageToChat($"[{DateTime.Now:HH:mm:ss}] Вы -> {selectedUser}: {message}");
                txtMessage.Clear();
            }
        }

        private void DisconnectFromServer()
        {
            if (_chatClient != null)
            {
                _chatClient.SaveChatHistory();
                _chatClient.Dispose();
                _chatClient = null;
            }

            UpdateConnectionState(false);
            lstUsers.Items.Clear();
            txtChat.Clear();
        }

        private void UpdateConnectionState(bool connected)
        {
            _isConnected = connected;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            txtNickname.Enabled = !connected;
            txtServerAddress.Enabled = !connected;
            txtPort.Enabled = !connected;
            txtMessage.Enabled = connected;
            btnSend.Enabled = connected;
            UpdateSendButtons();

            if (connected)
            {
                lblStatus.Text = "Подключен";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "Не подключен";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateSendButtons()
        {
            if (!_isConnected)
            {
                btnSendToAll.Visible = false;
                btnSendToUser.Visible = false;
                return;
            }

            if (lstUsers.SelectedItem != null)
            {
                btnSendToAll.Visible = true;
                btnSendToUser.Visible = true;
                btnSend.Visible = false;
            }
            else
            {
                btnSendToAll.Visible = false;
                btnSendToUser.Visible = false;
                btnSend.Visible = true;
            }
        }

        private void AddMessageToChat(string message)
        {
            if (txtChat.InvokeRequired)
            {
                txtChat.Invoke(new Action(() => AddMessageToChat(message)));
                return;
            }

            txtChat.AppendText(message + Environment.NewLine);
            txtChat.SelectionStart = txtChat.Text.Length;
            txtChat.ScrollToCaret();
        }

        private void LoadChatHistory()
        {
            if (_chatClient == null) return;

            var history = _chatClient.GetChatHistory();
            foreach (var entry in history.TakeLast(50)) // Показываем последние 50 сообщений
            {
                var timeStr = entry.Timestamp.ToString("HH:mm:ss");
                var deliveredStr = entry.IsDelivered ? "✓" : "⏳";
                AddMessageToChat($"[{timeStr}] {entry.SenderName}: {entry.Message} {deliveredStr}");
            }
        }

        // Обработчики событий клиента
        private void ChatClient_MessageReceived(object? sender, string message)
        {
            if (txtChat.InvokeRequired)
            {
                txtChat.Invoke(new Action(() => ChatClient_MessageReceived(sender, message)));
                return;
            }

            AddMessageToChat(message);
        }

        private void ChatClient_UserListUpdated(object? sender, List<string> users)
        {
            if (lstUsers.InvokeRequired)
            {
                lstUsers.Invoke(new Action(() => ChatClient_UserListUpdated(sender, users)));
                return;
            }

            lstUsers.Items.Clear();
            foreach (var user in users)
            {
                var display = user == txtNickname.Text ? user + " (вы)" : user;
                lstUsers.Items.Add(display);
            }
        }

        private void ChatClient_StatusChanged(object? sender, string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => ChatClient_StatusChanged(sender, status)));
                return;
            }

            lblStatus.Text = status;
        }

        private void ChatClient_MessageDelivered(object? sender, string messageId)
        {
             // изменить цвет последнего сообщения
        }

        // Элементы управления
        private TextBox txtNickname = null!;
        private TextBox txtServerAddress = null!;
        private TextBox txtPort = null!;
        private Button btnConnect = null!;
        private Button btnDisconnect = null!;
        private Label lblStatus = null!;
        private ListBox lstUsers = null!;
        private RichTextBox txtChat = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;
        private Button btnSendToAll = null!;
        private Button btnSendToUser = null!;

 
        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(958, 745);
            Name = "Form1";
            ResumeLayout(false);
        }
    }
}
