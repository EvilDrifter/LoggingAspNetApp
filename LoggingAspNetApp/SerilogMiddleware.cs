using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;

namespace LoggingAspNetApp
{
    class SerilogMiddleware
    {
        static readonly ILogger Log = Serilog.Log.ForContext<SerilogMiddleware>();

        readonly RequestDelegate _next;

        private readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SerilogMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var sw = Stopwatch.StartNew();
            try
            {
                var req = await GetRequest(httpContext.Request);
                Log.Write(LogEventLevel.Information, JsonSerializer.Serialize(req, _serializeOptions));

                var originalResponseBody = httpContext.Response.Body;
                await using var newResponseBody = new MemoryStream();
                httpContext.Response.Body = newResponseBody;

                await _next(httpContext);

                sw.Stop();

                newResponseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

                var res = GetResponse(httpContext.Response, sw.Elapsed.TotalMilliseconds, responseBodyText);
                var statusCode = httpContext.Response?.StatusCode;
                var level = statusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;
                Log.Write(level, JsonSerializer.Serialize(res, _serializeOptions));

                newResponseBody.Seek(0, SeekOrigin.Begin);
                await newResponseBody.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                var res = GetResponse(httpContext.Response, sw.Elapsed.TotalMilliseconds, ex.Message);
                Log.Write(LogEventLevel.Error, JsonSerializer.Serialize(res, _serializeOptions));
                Log.Write(LogEventLevel.Error, JsonSerializer.Serialize(ex, _serializeOptions));
            }
        }

        private async Task<ApiRequest> GetRequest(HttpRequest request)
        {
            using var sr = new StreamReader(request.Body);
            var result = new ApiRequest
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.Value,
                Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Scheme = request.Scheme,
                Host = request.Host.ToString(),
                Body = await sr.ReadToEndAsync()
            };

            return result;
        }

        private ApiResponse GetResponse(HttpResponse response, double totalMilliseconds, string body)
        {
            return new ApiResponse
            {
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                Body = body,
                TotalMilliseconds = Math.Round(totalMilliseconds, 3)
            };
        }
    }
}