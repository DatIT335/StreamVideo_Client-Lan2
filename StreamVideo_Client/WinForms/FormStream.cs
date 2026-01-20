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
        private TcpClientManager _clientManager;
        private FlowLayoutPanel _videoGrid;
        private PictureBox _pbServerScreen;

        // Webcam
        private FilterInfoCollection _filterInfoCollection;
        private VideoCaptureDevice _videoCaptureDevice;

        // Biến lưu trữ
        private bool _dangGhiHinh = true;
        private string _folderLuu;

        public FormStream(TcpClientManager clientManager)
        {
            _clientManager = clientManager;
            InitUI();

            // 1. Tự bật Webcam của mình
            StartWebcam();

            // 2. Đăng ký nhận ảnh từ người khác
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

        // Gửi ảnh gốc lên Server
        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (Bitmap frame = (Bitmap)eventArgs.Frame.Clone())
                {
                    using (Bitmap resized = new Bitmap(frame, new Size(640, 480)))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            resized.Save(ms, ImageFormat.Jpeg);
                            // GỬI LÊN SERVER (TcpClientManager đã có hàm này rồi)
                            _clientManager.SendVideoFrame(ms.ToArray());
                        }
                    }
                }
            }
            catch { }
        }

        // Hiển thị ảnh nhận được (ĐÃ BỎ GIẢI MÃ THỪA)
        private void HienThiAnh(byte[] imgData)
        {
            try
            {
                if (InvokeRequired) { Invoke(new Action<byte[]>(HienThiAnh), imgData); return; }

                if (imgData != null)
                {
                    using (MemoryStream ms = new MemoryStream(imgData))
                    {
                        Image newImg = Image.FromStream(ms); // Dùng thẳng imgData sạch
                        Image oldImg = _pbServerScreen.Image;
                        _pbServerScreen.Image = newImg;
                        if (oldImg != null) oldImg.Dispose();
                    }
                    if (_dangGhiHinh)
                    {
                        // Tạo tên file theo thời gian thực (để không bị trùng)
                        string filename = Path.Combine(_folderLuu, $"Frame_{DateTime.Now.Ticks}.jpg");

                        // Lưu dữ liệu ảnh xuống ổ cứng
                        File.WriteAllBytesAsync(filename, imgData);
                    }
                }
            }
            catch { }
        }

        // --- CÁC HÀM GIAO DIỆN CŨ (GIỮ NGUYÊN) ---
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
            _pbServerScreen.Width = 800;
            _pbServerScreen.Height = 450;
            _pbServerScreen.BackColor = Color.Black;
            _pbServerScreen.SizeMode = PictureBoxSizeMode.Zoom;
            _pbServerScreen.BorderStyle = BorderStyle.FixedSingle;
            _videoGrid.Controls.Add(_pbServerScreen);

            // Nút Kết thúc
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

        private void BtnEnd_Click(object sender, EventArgs e)
        {
            _dangGhiHinh = false;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning)
                _videoCaptureDevice.SignalToStop();
            base.OnFormClosing(e);
            _clientManager.Disconnect();
        }
    }
}