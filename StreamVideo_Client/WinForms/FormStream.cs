using StreamVideo_Client.Network;
using System;
using System.Drawing;
using System.Drawing.Imaging; // [Mới] Để xử lý ảnh
using System.IO;
using System.Windows.Forms;
using AForge.Video;           // [Mới] Webcam
using AForge.Video.DirectShow;// [Mới] Webcam

namespace StreamVideo_Client.WinForms
{
    public partial class FormStream : Form
    {
        private TcpClientManager _clientManager;
        private FlowLayoutPanel _videoGrid;
        private PictureBox _pbServerScreen;

        // Biến lưu trữ
        private bool _dangGhiHinh = true;
        private string _folderLuu;

        // [Mới] Biến Webcam
        private FilterInfoCollection _filterInfoCollection;
        private VideoCaptureDevice _videoCaptureDevice;

        public FormStream(TcpClientManager clientManager)
        {
            _clientManager = clientManager;
            InitUI();

            // Khởi động Webcam ngay khi vào phòng
            StartWebcam();

            // Đăng ký sự kiện nhận ảnh từ người khác
            _clientManager.OnVideoFrameReceived += HienThiAnh;
        }

        // [Mới] Hàm khởi động Webcam
        private void StartWebcam()
        {
            try
            {
                _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_filterInfoCollection.Count > 0)
                {
                    // Lấy camera đầu tiên tìm thấy
                    _videoCaptureDevice = new VideoCaptureDevice(_filterInfoCollection[0].MonikerString);
                    _videoCaptureDevice.NewFrame += Video_NewFrame; // Gắn hàm xử lý
                    _videoCaptureDevice.Start();
                }
                else
                {
                    MessageBox.Show("Không tìm thấy Camera!", "Lỗi");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi bật cam: " + ex.Message); }
        }

        // [Mới] Xử lý khi Webcam chụp được 1 khung hình -> Gửi đi
        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (Bitmap frame = (Bitmap)eventArgs.Frame.Clone())
                {
                    // Resize về 640x480 cho nhẹ mạng
                    using (Bitmap resized = new Bitmap(frame, new Size(640, 480)))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            resized.Save(ms, ImageFormat.Jpeg);
                            byte[] imgData = ms.ToArray();

                            // GỬI LÊN SERVER (Gửi ảnh gốc, chưa mã hóa)
                            // Server sẽ lo việc mã hóa AES khi gửi lại cho người khác
                            _clientManager.SendVideoFrame(imgData);
                        }
                    }
                }
            }
            catch { }
        }

        private void InitUI()
        {
            string folderGoc = Application.StartupPath;
            _folderLuu = Path.Combine(folderGoc, "Recordings", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(_folderLuu);

            this.Text = "Phòng họp trực tuyến - Đang Ghi Hình...";
            this.BackColor = Color.FromArgb(32, 33, 36);
            this.WindowState = FormWindowState.Maximized;

            _videoGrid = new FlowLayoutPanel();
            _videoGrid.Dock = DockStyle.Fill;
            _videoGrid.BackColor = Color.FromArgb(32, 33, 36);
            this.Controls.Add(_videoGrid);

            _pbServerScreen = new PictureBox();
            _pbServerScreen.Width = 800;
            _pbServerScreen.Height = 450;
            _pbServerScreen.BackColor = Color.Black;
            _pbServerScreen.SizeMode = PictureBoxSizeMode.Zoom;
            _pbServerScreen.Margin = new Padding(20);
            _pbServerScreen.BorderStyle = BorderStyle.FixedSingle;
            _videoGrid.Controls.Add(_pbServerScreen);

            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 80;
            pnlBottom.BackColor = Color.FromArgb(20, 20, 20);
            this.Controls.Add(pnlBottom);

            Button btnEnd = new Button();
            btnEnd.Text = "KẾT THÚC";
            btnEnd.BackColor = Color.Red;
            btnEnd.ForeColor = Color.White;
            btnEnd.Size = new Size(120, 40);
            btnEnd.Location = new Point((Screen.PrimaryScreen.Bounds.Width / 2) - 60, 20);
            btnEnd.Click += BtnEnd_Click;
            pnlBottom.Controls.Add(btnEnd);
        }

        // Hàm hiển thị ảnh NHẬN ĐƯỢC từ người khác
        private void HienThiAnh(byte[] imgData)
        {
            try
            {
                if (InvokeRequired) { Invoke(new Action<byte[]>(HienThiAnh), imgData); return; }

                // --- ĐÃ SỬA: BỎ GIẢI MÃ THỪA ---
                // Trước đây có dòng SecurityHelper.Decrypt(imgData) -> Đã xóa.
                // Vì TcpClientManager đã giải mã bằng AesHelper rồi, nên imgData ở đây là ảnh sạch.

                if (imgData != null)
                {
                    using (MemoryStream ms = new MemoryStream(imgData))
                    {
                        Image newImg = Image.FromStream(ms);
                        Image oldImg = _pbServerScreen.Image;
                        _pbServerScreen.Image = newImg;
                        if (oldImg != null) oldImg.Dispose();
                    }

                    if (_dangGhiHinh)
                    {
                        string filename = Path.Combine(_folderLuu, $"Frame_{DateTime.Now.Ticks}.jpg");
                        File.WriteAllBytesAsync(filename, imgData);
                    }
                }
            }
            catch { }
        }

        private void BtnEnd_Click(object sender, EventArgs e)
        {
            _dangGhiHinh = false;
            MessageBox.Show($"Cuộc gọi kết thúc.\nDữ liệu đã lưu tại:\n{_folderLuu}", "Thông báo");
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // [Mới] Tắt Webcam khi đóng form
            if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning)
            {
                _videoCaptureDevice.SignalToStop();
            }

            base.OnFormClosing(e);
            if (_clientManager != null)
            {
                _clientManager.OnVideoFrameReceived -= HienThiAnh;
                _clientManager.Disconnect();
            }
        }
    }
}