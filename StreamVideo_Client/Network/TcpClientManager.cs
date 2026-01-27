using StreamVideo_Client.DTO;
using StreamVideo_Client.Common;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Net.Security;
using NAudio.Wave; // Thư viện âm thanh
using System.Diagnostics;

namespace StreamVideo_Client.Network
{
    public class TcpClientManager
    {
        private TcpClient _client;
        private SslStream _sslStream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private Thread _listenThread;

        public event Action<string, byte[]> OnVideoFrameReceived;

        // --- ÂM THANH (LOA & MIC) ---
        private BufferedWaveProvider _waveProvider; // Để phát loa
        private WaveOutEvent _waveOut;
        private WaveInEvent _waveIn; // Để thu âm Mic

        // Biến kiểm soát Mic (Để FormStream chỉnh)
        public bool IsMicEnabled { get; set; } = true;

        private AutoResetEvent _loginWaitHandle = new AutoResetEvent(false);
        private bool _lastLoginResult = false;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);

                // Bỏ qua check SSL
                _sslStream = new SslStream(_client.GetStream(), false, (s, c, ch, e) => true);
                _sslStream.AuthenticateAsClient("StreamServer");

                _reader = new BinaryReader(_sslStream);
                _writer = new BinaryWriter(_sslStream);

                // --- 1. SETUP LOA (NGHE) ---
                // SỬA: Dùng 8000Hz cho đồng bộ với Mic
                _waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 1));
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_waveProvider);
                _waveOut.Play();

                // --- 2. SETUP MIC (NÓI) ---
                try
                {
                    _waveIn = new WaveInEvent();
                    // SỬA QUAN TRỌNG: Giảm xuống 8000Hz để nhẹ mạng, tránh lag video
                    _waveIn.WaveFormat = new WaveFormat(8000, 1);

                    // SỬA: Tăng buffer lên 100ms để giảm tải CPU
                    _waveIn.BufferMilliseconds = 100;

                    _waveIn.DataAvailable += OnMicDataAvailable;
                    _waveIn.StartRecording();
                }
                catch { Debug.WriteLine("Không tìm thấy Microphone!"); }

                _listenThread = new Thread(ListenLoop);
                _listenThread.IsBackground = true;
                _listenThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi kết nối: " + ex.Message);
                return false;
            }
        }

        // --- HÀM XỬ LÝ KHI MIC THU ĐƯỢC TIẾNG ---
        // Trong file TcpClientManager.cs
        private void OnMicDataAvailable(object sender, WaveInEventArgs e)
        {
            // --- QUAN TRỌNG: Dòng này chặn gửi âm thanh khi tắt Mic ---
            if (IsMicEnabled == false) return;
            // ----------------------------------------------------------

            try
            {
                if (e.BytesRecorded > 0)
                {
                    byte[] audioData = new byte[e.BytesRecorded];
                    Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                    GuiDuLieu(3, audioData);
                }
            }
            catch { }
        }

        private void ListenLoop()
        {
            try
            {
                while (_client.Connected)
                {
                    int length = _reader.ReadInt32();
                    byte type = _reader.ReadByte();
                    byte[] payload = _reader.ReadBytes(length);

                    if (type == 1) // LOGIN RESPONSE
                    {
                        string json = Encoding.UTF8.GetString(payload);
                        var res = JsonSerializer.Deserialize<LoginResponseDTO>(json);
                        _lastLoginResult = res.ThanhCong;
                        _loginWaitHandle.Set();
                    }
                    else if (type == 2) // VIDEO FRAME
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(payload))
                            using (BinaryReader br = new BinaryReader(ms))
                            {
                                // A. Đọc độ dài tên (4 byte đầu)
                                int nameLen = br.ReadInt32();

                                // B. Đọc tên người gửi
                                byte[] nameBytes = br.ReadBytes(nameLen);
                                string senderName = Encoding.UTF8.GetString(nameBytes);

                                // C. Đọc dữ liệu ảnh (phần còn lại)
                                int imageLen = (int)(ms.Length - ms.Position);
                                byte[] videoCipher = br.ReadBytes(imageLen);

                                // D. Giải mã và bắn Event kèm Tên
                                byte[] decryptedImage = AesHelper.Decrypt(videoCipher);
                                OnVideoFrameReceived?.Invoke(senderName, decryptedImage);
                            }
                        }
                        catch { }
                    }
                    else if (type == 3) // AUDIO STREAM (LOA)
                    {
                        if (_waveProvider != null)
                        {
                            _waveProvider.AddSamples(payload, 0, payload.Length);
                        }
                    }
                }
            }
            catch
            {
                Disconnect();
            }
        }

        public void SendVideoFrame(byte[] data)
        {
            if (_client != null && _client.Connected)
            {
                // Gửi Type 2 (Video).
                GuiDuLieu(2, data);
            }
        }

        public bool Login(string user, string pass)
        {
            if (_client == null || !_client.Connected) return false;
            var req = new BaseRequestDTO
            {
                Type = RequestType.LOGIN,
                Payload = JsonSerializer.Serialize(new LoginRequestDTO { TenDangNhap = user, MatKhau = pass })
            };
            GuiDuLieu(1, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req)));
            _loginWaitHandle.WaitOne(3000);
            return _lastLoginResult;
        }

        public void GuiDuLieu(byte type, byte[] data)
        {
            try
            {
                lock (_writer)
                {
                    _writer.Write((int)data.Length);
                    _writer.Write(type);
                    _writer.Write(data);
                    _writer.Flush();
                }
            }
            catch { }
        }

        public void Disconnect()
        {
            try
            {
                // Tắt Mic
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                    _waveIn = null;
                }

                // Tắt Loa
                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _waveOut.Dispose();
                    _waveOut = null;
                }

                _client?.Close();
            }
            catch { }
        }
    }
}