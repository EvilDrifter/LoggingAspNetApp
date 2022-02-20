using System.Collections.Generic;

namespace LoggingAspNetApp
{
    public class ApiRequest
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public string QueryString { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string Scheme { get; set; }

        public string Host { get; set; }

        public string Body { get; set; }
    }
}