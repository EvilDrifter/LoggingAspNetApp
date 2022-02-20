using System.Collections.Generic;

namespace LoggingAspNetApp
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string Body { get; set; }

        public double TotalMilliseconds { get; set; }
    }
}