namespace PCBlockerUtility
{
    public partial class PasswordForm : Form
    {
        public string Password { get; private set; }

        public PasswordForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Введите пароль";
            this.Size = new System.Drawing.Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label label = new Label
            {
                Text = "Пароль:",
                Location = new System.Drawing.Point(10, 20),
                AutoSize = true
            };

            TextBox passwordTextBox = new TextBox
            {
                UseSystemPasswordChar = true,
                Location = new System.Drawing.Point(10, 50),
                Width = 260
            };

            Button confirmButton = new Button
            {
                Text = "Подтвердить",
                Location = new System.Drawing.Point(70, 100),
                DialogResult = DialogResult.OK,
                Size = new System.Drawing.Size(140, 40)
            };

            confirmButton.Click += (s, e) =>
            {
                Password = passwordTextBox.Text;
            };

            this.Controls.Add(label);
            this.Controls.Add(passwordTextBox);
            this.Controls.Add(confirmButton);
            this.AcceptButton = confirmButton;
        }
    }
}