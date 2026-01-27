using StreamVideo_Client.DTO;
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace StreamVideo_Client
{
    public class FormRegister : Form
    {
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnRegister;
        private Button btnCancel;
        private Label lblTitle;
        private Label lblUser;
        private Label lblPass;

        public FormRegister()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            // 1. Form gọn hơn (giảm chiều cao)
            this.Text = "Đăng Ký Tài Khoản";
            this.Size = new Size(400, 380); // Giảm chiều cao
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(32, 33, 36);

            Font fontLabel = new Font("Segoe UI", 10, FontStyle.Regular);
            Font fontInput = new Font("Segoe UI", 11, FontStyle.Regular);
            Color colorText = Color.WhiteSmoke;
            Color colorInputBg = Color.FromArgb(48, 50, 56);

            // Tiêu đề
            lblTitle = new Label();
            lblTitle.Text = "ĐĂNG KÝ TÀI KHOẢN";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(90, 30);
            this.Controls.Add(lblTitle);

            // Ô Tài khoản
            lblUser = new Label { Text = "Tài khoản:", ForeColor = colorText, Font = fontLabel, Location = new Point(40, 80), AutoSize = true };
            txtUser = new TextBox { Location = new Point(40, 105), Size = new Size(300, 30), Font = fontInput, BackColor = colorInputBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);

            // Ô Mật khẩu
            lblPass = new Label { Text = "Mật khẩu:", ForeColor = colorText, Font = fontLabel, Location = new Point(40, 150), AutoSize = true };
            txtPass = new TextBox { Location = new Point(40, 175), Size = new Size(300, 30), Font = fontInput, BackColor = colorInputBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);

            // Nút ĐĂNG KÝ (Đẩy lên cao hơn do bỏ ô FullName)
            btnRegister = new Button();
            btnRegister.Text = "XÁC NHẬN ĐĂNG KÝ";
            btnRegister.Size = new Size(300, 45);
            btnRegister.Location = new Point(40, 230); // Vị trí mới
            btnRegister.BackColor = Color.FromArgb(40, 167, 69);
            btnRegister.ForeColor = Color.White;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnRegister.Cursor = Cursors.Hand;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;
            this.Controls.Add(btnRegister);

            // Nút QUAY LẠI
            btnCancel = new Button();
            btnCancel.Text = "QUAY LẠI";
            btnCancel.Size = new Size(300, 45);
            btnCancel.Location = new Point(40, 290); // Vị trí mới
            btnCancel.BackColor = Color.FromArgb(220, 53, 69);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                MessageBox.Show("Vui lòng nhập tài khoản và mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnRegister.Text = "Đang xử lý...";
                btnRegister.Enabled = false;

                // 1. Kết nối TCP (Nhớ nhập đúng IP Server của m)
                TcpClient tempClient = new TcpClient("192.168.123.8", 9000);
                NetworkStream netStream = tempClient.GetStream();

                // --- ĐOẠN MỚI: BẮT TAY BẢO MẬT SSL ---
                // Server đòi SSL thì mình phải chiều
                SslStream sslStream = new SslStream(netStream, false, new RemoteCertificateValidationCallback((s, c, ch, err) => true));
                sslStream.AuthenticateAsClient("StreamServer");
                // -------------------------------------

                // 2. Tạo gói tin
                var regInfo = new RegisterRequestDTO
                {
                    Username = txtUser.Text.Trim(),
                    Password = txtPass.Text.Trim()
                };

                var request = new BaseRequestDTO
                {
                    Type = RequestType.REGISTER,
                    Payload = JsonSerializer.Serialize(regInfo)
                };

                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

                // 3. Gửi đi (Ghi vào sslStream chứ không phải netStream)
                BinaryWriter writer = new BinaryWriter(sslStream);
                writer.Write(jsonBytes.Length);
                writer.Write((byte)1);
                writer.Write(jsonBytes);
                writer.Flush();

                // 4. Nhận phản hồi
                BinaryReader reader = new BinaryReader(sslStream);
                int len = reader.ReadInt32();
                byte type = reader.ReadByte();
                byte[] respBytes = reader.ReadBytes(len);

                var response = JsonSerializer.Deserialize<LoginResponseDTO>(Encoding.UTF8.GetString(respBytes));

                MessageBox.Show(response.ThongBao, "Thông báo", MessageBoxButtons.OK, response.ThanhCong ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                if (response.ThanhCong) this.Close();

                // Đóng kết nối an toàn
                sslStream.Close();
                tempClient.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRegister.Text = "XÁC NHẬN ĐĂNG KÝ";
                btnRegister.Enabled = true;
            }
        }
    }
}