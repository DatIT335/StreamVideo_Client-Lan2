using StreamVideo_Client.Network;
using StreamVideo_Client.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace StreamVideo_Client.WinForms
{
    public partial class FormConnect : Form
    {
        private TextBox txtIP, txtPort, txtUser, txtPass;
        private Button btnLogin, btnRegister, btnExit, btnEye; // Thêm btnRegister
        private CheckBox chkRemember;
        private Label lblStatus;
        private TcpClientManager _clientManager;

        public FormConnect()
        {
            InitUI();
            LoadSettings();
        }

        private void InitUI()
        {
            // Cấu hình Form
            this.Text = "Đăng nhập hệ thống Stream";
            this.Size = new Size(400, 600); // Tăng chiều cao lên chút
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.DoubleBuffered = true;

            // Tiêu đề
            Label lblTitle = new Label();
            lblTitle.Text = "VIDEO STREAM LOGIN";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 80;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // Panel chứa nội dung
            Panel pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(40, 0, 40, 0);
            this.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            // Flow Layout
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoSize = true;

            // Tạo các control
            Label lblIP = CreateLabel("IP Server:");
            txtIP = CreateTextBox("127.0.0.1");
            Label lblPort = CreateLabel("Port:");
            txtPort = CreateTextBox("9000");
            Label lblUser = CreateLabel("Tài khoản:");
            txtUser = CreateTextBox("");
            Label lblPass = CreateLabel("Mật khẩu:");
            txtPass = CreateTextBox("");
            txtPass.UseSystemPasswordChar = true;

            // Nút mắt thần
            btnEye = new Button();
            btnEye.Text = "👁";
            btnEye.Size = new Size(30, 28);
            btnEye.FlatStyle = FlatStyle.Flat;
            btnEye.FlatAppearance.BorderSize = 0;
            btnEye.ForeColor = Color.Gray;
            btnEye.BackColor = Color.FromArgb(45, 45, 48);
            btnEye.Click += (s, e) => {
                txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
                btnEye.Text = txtPass.UseSystemPasswordChar ? "👁" : "🙈";
            };

            Panel pnlPass = new Panel();
            pnlPass.Size = new Size(320, 35);
            txtPass.Location = new Point(0, 0);
            txtPass.Width = 280;
            btnEye.Location = new Point(285, 0);
            pnlPass.Controls.Add(txtPass);
            pnlPass.Controls.Add(btnEye);

            chkRemember = new CheckBox();
            chkRemember.Text = "Ghi nhớ thông tin";
            chkRemember.ForeColor = Color.LightGray;
            chkRemember.AutoSize = true;

            lblStatus = new Label();
            lblStatus.ForeColor = Color.Red;
            lblStatus.AutoSize = false;
            lblStatus.Height = 30;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // --- CÁC NÚT BẤM ---
            btnLogin = CreateButton("ĐĂNG NHẬP", Color.FromArgb(0, 122, 204));
            btnLogin.Click += BtnLogin_Click;

            // Nút Đăng ký (Mới)
            btnRegister = CreateButton("ĐĂNG KÝ TÀI KHOẢN", Color.FromArgb(40, 167, 69)); // Màu xanh lá
            btnRegister.Click += BtnRegister_Click;

            btnExit = CreateButton("THOÁT", Color.FromArgb(192, 57, 43));
            btnExit.Click += (s, e) => Application.Exit();

            // Thêm vào Flow theo thứ tự
            flow.Controls.Add(lblIP); flow.Controls.Add(txtIP);
            flow.Controls.Add(lblPort); flow.Controls.Add(txtPort);
            flow.Controls.Add(lblUser); flow.Controls.Add(txtUser);
            flow.Controls.Add(lblPass); flow.Controls.Add(pnlPass);
            flow.Controls.Add(chkRemember);
            flow.Controls.Add(new Panel { Height = 10 });
            flow.Controls.Add(btnLogin); // 1. Login
            flow.Controls.Add(new Panel { Height = 5 });
            flow.Controls.Add(btnRegister); // 2. Register
            flow.Controls.Add(new Panel { Height = 5 });
            flow.Controls.Add(btnExit);  // 3. Exit
            flow.Controls.Add(lblStatus);

            pnlContent.Controls.Add(flow);

            // Căn lề
            foreach (Control c in flow.Controls)
            {
                if (c is TextBox || c is Button || c is Panel) c.Margin = new Padding(0, 0, 0, 10);
                if (c is Label) c.Margin = new Padding(0, 5, 0, 2);
            }
        }

        // --- Logic Đăng Ký ---
        private void BtnRegister_Click(object sender, EventArgs e)
        {
            // Tạm thời hiện thông báo, sau này sẽ mở FormRegister
            MessageBox.Show("Tính năng đăng ký đang được phát triển!\nVui lòng liên hệ Admin để cấp tài khoản.", "Thông báo");
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                lblStatus.Text = "⚠️ Thiếu thông tin!"; return;
            }

            lblStatus.Text = "⏳ Đang kết nối...";
            lblStatus.ForeColor = Color.Yellow;
            Application.DoEvents();

            try
            {
                SaveSettings();
                _clientManager = new TcpClientManager();
                if (_clientManager.Connect(txtIP.Text.Trim(), int.Parse(txtPort.Text.Trim())))
                {
                    if (_clientManager.Login(txtUser.Text, txtPass.Text))
                    {
                        this.Hide();
                        // Mở FormStream mới
                        FormStream frm = new FormStream(_clientManager);
                        frm.ShowDialog();
                        this.Close();
                    }
                    else
                    {
                        lblStatus.Text = "❌ Sai tài khoản/mật khẩu!";
                        lblStatus.ForeColor = Color.Red;
                        _clientManager.Disconnect();
                    }
                }
                else
                {
                    lblStatus.Text = "❌ Lỗi kết nối Server!";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Lỗi: " + ex.Message;
            }
        }

        // Helper tạo control
        private Label CreateLabel(string text) => new Label() { Text = text, ForeColor = Color.LightGray, Font = new Font("Segoe UI", 10), AutoSize = true };
        private TextBox CreateTextBox(string txt) => new TextBox() { Text = txt, Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Width = 320, Height = 30 };
        private Button CreateButton(string text, Color color)
        {
            Button btn = new Button(); btn.Text = text; btn.BackColor = color; btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat; btn.FlatAppearance.BorderSize = 0; btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.Size = new Size(320, 40); btn.Cursor = Cursors.Hand; return btn;
        }

        // Settings
        private void LoadSettings()
        {
            try
            {
                txtIP.Text = string.IsNullOrEmpty(Settings.Default.LastIP) ? "127.0.0.1" : Settings.Default.LastIP;
                txtPort.Text = string.IsNullOrEmpty(Settings.Default.LastPort) ? "9000" : Settings.Default.LastPort;
                txtUser.Text = Settings.Default.LastUser;
                if (!string.IsNullOrEmpty(txtUser.Text)) chkRemember.Checked = true;
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                if (chkRemember.Checked) { Settings.Default.LastIP = txtIP.Text; Settings.Default.LastPort = txtPort.Text; Settings.Default.LastUser = txtUser.Text; }
                else { Settings.Default.LastUser = ""; }
                Settings.Default.Save();
            }
            catch { }
        }
    }
}