using PCBlockerUtility.Models;

namespace PCBlockerUtility
{
    partial class ChatForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private RichTextBox chatBox;
        private TextBox inputBox;
        private Button sendButton;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Чат поддержки с AI";
            this.Size = new Size(500, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Chat box
            chatBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Margin = new Padding(10),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Input label
            var inputLabel = new Label
            {
                Text = "Введите ваш вопрос:",
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Bottom
            };

            // Input box
            inputBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Margin = new Padding(10, 0, 10, 10),
                Multiline = true
            };

            // Send button
            var sendButton = new Button
            {
                Text = "Отправить",
                Dock = DockStyle.Bottom,
                Height = 40,
                Margin = new Padding(10, 0, 10, 10)
            };

            inputBox.KeyDown += async (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await ProcessUserMessage();
                }
            };

            sendButton.Click += async (sender, e) => await ProcessUserMessage();

            this.Controls.Add(chatBox);
            this.Controls.Add(inputLabel);
            this.Controls.Add(inputBox);
            this.Controls.Add(sendButton);
        }

        private async Task ProcessUserMessage()
        {
            var userMessage = inputBox.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            _chatHistory.Add(new ChatMessage { Role = "user", Content = userMessage });
            chatBox.AppendText($"Вы: {userMessage}\n\n");
            inputBox.Clear();
            inputBox.Enabled = false;

            try
            {
                string typingMessage = "Подождите, печатаю...";
                chatBox.AppendText($"Поддержка: {typingMessage}\n\n");

                int startIndex = chatBox.Text.Length;

                var aiResponse = await _chatClient.GetAIResponseAsync(_chatHistory);

                chatBox.Select(startIndex, typingMessage.Length + $"Поддержка: ".Length);
                chatBox.SelectedText = "";
                chatBox.AppendText($"Поддержка: {aiResponse}\n\n");

                _chatHistory.Add(new ChatMessage { Role = "assistant", Content = aiResponse });
            }
            catch (Exception ex)
            {
                chatBox.AppendText($"Ошибка: {ex.Message}\n\n");
            }
            finally
            {
                inputBox.Enabled = true;
                inputBox.Focus();
            }
        }

        #endregion
    }
}