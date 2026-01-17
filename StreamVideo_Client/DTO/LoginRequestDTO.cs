using System;
using System.Collections.Generic;
using System.Text;

namespace StreamVideo_Client.DTO
{
    public class LoginRequestDTO
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
    }
    public enum RequestType
    {
        LOGIN,
        STREAM,
        LOGOUT
    }
    public class BaseRequestDTO
    {
        public RequestType Type { get; set; }
        public string Payload { get; set; }
    }
}

