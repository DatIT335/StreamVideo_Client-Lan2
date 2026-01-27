using System;
using System.Collections.Generic;
using System.Text;

namespace StreamVideo_Client.DTO
{
    public class BaseRequestDTO
    {  
            public RequestType Type { get; set; }
            public string Payload { get; set; }
    }
}
