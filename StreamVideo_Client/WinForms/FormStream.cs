using StreamVideo_Client.Network;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

namespace StreamVideo_Client.WinForms
{
    public partial class FormStream : Form
    {
        // Thêm vào đầu class FormStream
        private Dictionary<string, PictureBox> _clientScreens = new Dictionary<string, PictureBox>();
        private TcpClientManager _clientManager;
        private FlowLayoutPanel _videoGrid;
        private PictureBox _pbServerScreen;

        // Các nút điều khiển
        private Button btnMic, btnCam, btnEnd;

        // Webcam
        private FilterInfoCollection _filterInfoCollection;
        private VideoCaptureDevice _videoCaptureDevice;

        // Biến trạng thái
        private bool _dangGhiHinh = true;
        private string _folderLuu;

        // --- TRẠNG THÁI MIC/CAM MỚI ---
        private bool _isMicOn = true;
        private bool _isCamOn = true;

        public FormStream(TcpClientManager clientManager)
        {
            _clientManager = clientManager;

            // Cấu hình giao diện và Webcam
            InitUI();
            StartWebcam();

            // Đăng ký nhận ảnh từ Server
            _clientManager.OnVideoFrameReceived += HienThiAnh;
        }

        private void StartWebcam()
        {
            try
            {
                _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_filterInfoCollection.Count > 0)
                {
                    _videoCaptureDevice = new VideoCaptureDevice(_filterInfoCollection[0].MonikerString);
                    _videoCaptureDevice.NewFrame += Video_NewFrame;
                    _videoCaptureDevice.Start();
                }
            }
            catch { MessageBox.Show("Không tìm thấy Camera!"); }
        }

        // --- GỬI ẢNH LÊN SERVER ---
        // Thay thế hàm Video_NewFrame cũ bằng hàm này:
        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Clone ảnh ra để xử lý (tránh lỗi bộ nhớ)
                using (Bitmap frame = (Bitmap)eventArgs.Frame.Clone())
                {
                    // --- 1. HIỂN THỊ HÌNH (LOCAL PREVIEW) - PHẢI DÙNG INVOKE ---
                    if (_pbServerScreen.InvokeRequired)
                    {
                        _pbServerScreen.Invoke(new Action(() =>
                        {
                            if (_isCamOn)
                            {
                                Image old = _pbServerScreen.Image;
                                _pbServerScreen.Image = (Bitmap)frame.Clone();
                                if (old != null) old.Dispose();
                            }
                            else
                            {
                                _pbServerScreen.Image = null; // Tắt cam thì hiện đen
                            }
                        }));
                    }
                    else
                    {
                        // Trường hợp hiếm (đã ở UI thread)
                        if (_isCamOn) _pbServerScreen.Image = (Bitmap)frame.Clone();
                        else _pbServerScreen.Image = null;
                    }

                    // --- 2. GỬI LÊN SERVER ---
                    // Chỉ gửi khi Cam đang bật
                    if (_isCamOn)
                    {
                        using (Bitmap resized = new Bitmap(frame, new Size(640, 480)))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                resized.Save(ms, ImageFormat.Jpeg);
                                _clientManager.SendVideoFrame(ms.ToArray());
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // --- HIỂN THỊ ẢNH TỪ SERVER ---
        // [FormStream.cs]

        // Hàm nhận thêm tham số senderName
        private void HienThiAnh(string senderName, byte[] imgData)
        {
            try
            {
                // Bắt buộc Invoke để thao tác UI
                if (InvokeRequired)
                {
                    Invoke(new Action<string, byte[]>(HienThiAnh), senderName, imgData);
                    return;
                }

                if (imgData == null) return;

                PictureBox targetBox = null;

                // 1. Nếu là Server gửi -> Hiện lên màn hình to chính giữa
                if (senderName == "Server")
                {
                    targetBox = _pbServerScreen;
                }
                // 2. Nếu là Client khác gửi
                else
                {
                    // Kiểm tra xem đã có ô cho người này chưa
                    if (!_clientScreens.ContainsKey(senderName))
                    {
                        // Chưa có -> Tạo ô mới
                        PictureBox newBox = new PictureBox();
                        newBox.Size = new Size(200, 150); // Kích thước ô nhỏ
                        newBox.SizeMode = PictureBoxSizeMode.Zoom;
                        newBox.BackColor = Color.Black;
                        newBox.BorderStyle = BorderStyle.FixedSingle;
                        newBox.Margin = new Padding(5);

                        // Hiển thị tên người dùng đè lên góc
                        Label lblName = new Label();
                        lblName.Text = senderName;
                        lblName.ForeColor = Color.Yellow;
                        lblName.BackColor = Color.Transparent;
                        lblName.Parent = newBox;
                        lblName.Location = new Point(5, 5);

                        // Thêm vào FlowLayoutPanel (_videoGrid)
                        _videoGrid.Controls.Add(newBox);

                        // Lưu vào danh sách để dùng lại lần sau
                        _clientScreens.Add(senderName, newBox);
                    }

                    // Lấy ô đã có
                    targetBox = _clientScreens[senderName];
                }

                // 3. Hiển thị ảnh lên ô đã xác định
                using (MemoryStream ms = new MemoryStream(imgData))
                {
                    Image oldImg = targetBox.Image;
                    targetBox.Image = Image.FromStream(ms);
                    if (oldImg != null) oldImg.Dispose(); // Giải phóng RAM
                }
            }
            catch { }
        }

        // --- GIAO DIỆN (ĐÃ NÂNG CẤP THÊM NÚT) ---
        private void InitUI()
        {
            string folderGoc = Application.StartupPath;
            _folderLuu = Path.Combine(folderGoc, "Recordings", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(_folderLuu);

            this.Text = "Phòng họp trực tuyến";
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.WindowState = FormWindowState.Maximized;

            _videoGrid = new FlowLayoutPanel();
            _videoGrid.Dock = DockStyle.Fill;
            this.Controls.Add(_videoGrid);

            _pbServerScreen = new PictureBox();
            _pbServerScreen.Width = 800; // Có thể chỉnh to hơn nếu muốn
            _pbServerScreen.Height = 450;
            _pbServerScreen.BackColor = Color.Black;
            _pbServerScreen.SizeMode = PictureBoxSizeMode.Zoom;
            _pbServerScreen.BorderStyle = BorderStyle.FixedSingle;
            _videoGrid.Controls.Add(_pbServerScreen);

            // --- THANH ĐIỀU KHIỂN BÊN DƯỚI ---
            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 80;
            pnlBottom.BackColor = Color.FromArgb(20, 20, 20);
            this.Controls.Add(pnlBottom);

            // Tính vị trí giữa màn hình
            int centerX = Screen.PrimaryScreen.Bounds.Width / 2;
            int btnY = 20;

            // 1. Nút LOA (Bên trái)
            btnMic = new Button();
            btnMic.Text = "🔊 TẮT MIC";
            btnMic.BackColor = Color.FromArgb(60, 64, 67); // Màu xám
            btnMic.ForeColor = Color.White;
            btnMic.Size = new Size(120, 40);
            btnMic.Location = new Point(centerX - 200, btnY);
            btnMic.FlatStyle = FlatStyle.Flat;
            btnMic.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnMic.Click += BtnMic_Click;
            pnlBottom.Controls.Add(btnMic);

            // 2. Nút KẾT THÚC (Ở giữa - Giữ nguyên của m)
            btnEnd = new Button();
            btnEnd.Text = "KẾT THÚC";
            btnEnd.BackColor = Color.FromArgb(220, 53, 69); // Màu đỏ
            btnEnd.ForeColor = Color.White;
            btnEnd.Size = new Size(120, 40);
            btnEnd.Location = new Point(centerX - 60, btnY);
            btnEnd.FlatStyle = FlatStyle.Flat;
            btnEnd.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnEnd.Click += BtnEnd_Click;
            pnlBottom.Controls.Add(btnEnd);

            // 3. Nút CAM (Bên phải)
            btnCam = new Button();
            btnCam.Text = "📷 TẮT CAM";
            btnCam.BackColor = Color.FromArgb(60, 64, 67);
            btnCam.ForeColor = Color.White;
            btnCam.Size = new Size(120, 40);
            btnCam.Location = new Point(centerX + 80, btnY);
            btnCam.FlatStyle = FlatStyle.Flat;
            btnCam.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCam.Click += BtnCam_Click;
            pnlBottom.Controls.Add(btnCam);
        }

        // --- XỬ LÝ SỰ KIỆN NÚT BẤM ---

        private void BtnMic_Click(object sender, EventArgs e)
        {
            _isMicOn = !_isMicOn; // Đảo trạng thái

            if (_isMicOn)
            {
                btnMic.Text = "🔊 TẮT MIC";
                btnMic.BackColor = Color.FromArgb(60, 64, 67);
                // Ở đây m cần thêm logic bật Audio trong TcpClientManager nếu có
            }
            else
            {
                btnMic.Text = "🔇 BẬT MIC";
                btnMic.BackColor = Color.FromArgb(220, 53, 69); // Đỏ cảnh báo
                // Ở đây m cần thêm logic tắt Audio
            }

            // Cập nhật trạng thái cho ClientManager biết (Nếu m đã code phần Audio)
            _clientManager.IsMicEnabled = _isMicOn;
        }

        private void BtnCam_Click(object sender, EventArgs e)
        {
            _isCamOn = !_isCamOn; // Đảo trạng thái

            if (_isCamOn)
            {
                btnCam.Text = "📷 TẮT CAM";
                btnCam.BackColor = Color.FromArgb(60, 64, 67);
            }
            else
            {
                btnCam.Text = "🚫 BẬT CAM";
                btnCam.BackColor = Color.FromArgb(220, 53, 69);
            }
        }

        private void BtnEnd_Click(object sender, EventArgs e)
        {
            _dangGhiHinh = false;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning)
                _videoCaptureDevice.SignalToStop();

            _clientManager.Disconnect();
            base.OnFormClosing(e);
        }
    }
}