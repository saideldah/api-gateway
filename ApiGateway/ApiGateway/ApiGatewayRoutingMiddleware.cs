using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ApiGateway
{
    public class ApiGatewayRoutingMiddleware
    {
        private const string CONTENT_LENGTH = "Content-Length";
        private const string CONTENT_TYPE = "Content-Type";
        private readonly RequestDelegate _next;
        private readonly Dictionary<string, string> _apps = new Dictionary<string, string>
        {
            ["app1"] = "http://172.26.194.12",
            ["app2"] = "www.app2.com",
            ["app3"] = "www.app3.com",
            ["app4"] = "www.app4.com"
        };
        public ApiGatewayRoutingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            var path = await GetPath(context.Request.Path);
            var pathValues = path.Split("/");
            var res = new StringBuilder();
            var serviceName = await GetServiceName(path);
            res.AppendLine($@"path = {path}");
            res.AppendLine($@"AppName = {serviceName}");

            var serviceUri = await GetServiceUri(serviceName, path);

            if (!string.IsNullOrWhiteSpace(serviceUri))
            {

                res.AppendLine($"redirectUri = {serviceUri}");
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(2)
                };
                HttpResponseMessage response;
                switch (context.Request.Method)
                {
                    case "GET":
                        response = await httpClient.GetAsync(serviceUri);
                        break;
                    case "POST":
                        response = await httpClient.PostAsync(serviceUri, new StreamContent(context.Request.Body));
                        break;
                    case "PUT":
                        response = await httpClient.PutAsync(serviceUri, new StreamContent(context.Request.Body));
                        break;
                    case "DELETE":
                        response = await httpClient.DeleteAsync(serviceUri);
                        break;
                    default:
                        //To Do: implement patch, head and connect
                        response = new HttpResponseMessage();
                        break;
                }
                context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
                context.Response.ContentLength = response.Content.Headers.ContentLength;
                context.Response.StatusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    await context.Response.WriteAsync("err");
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("not found");
            }
            await _next(context);
        }
        private async Task<string> GetPath(PathString path)
        {
            // to do make it reg ex
            var p = path != null ? path.Value.ToString() : string.Empty;
            p = p.Remove(0, 1);
            return p;
        }
        private async Task<string> GetServiceName(string path)
        {
            // to do make it reg ex
            var pathValues = path.Split("/");
            return pathValues[0];
        }
        private async Task<string> GetServiceUri(string serviceName, string path)
        {

            path = "$" + path;
            // to do make it reg ex
            if (_apps.Keys.Any(k => k == serviceName))
            {
                var serviceUri = path.Replace($"${serviceName}", _apps[serviceName]);
                return serviceUri;
            }
            else
            {
                return null;
            }
        }

        private async Task AddRequestHeaders(HttpRequestHeaders headers, IHeaderDictionary requestHeaders, string serviceName)
        {
            foreach (var item in requestHeaders)
            {
                string[] value = item.Key.Equals("Host") ? new string[] { _apps[serviceName] } : item.Value.ToArray();
                headers.TryAddWithoutValidation(item.Key, value);
            }
        }
        private async Task AddRequest(HttpRequestMessage message, HttpRequest request)
        {
            if (!request.Method.ToUpperInvariant().Equals("GET"))
            {
                string contentLength = request.Headers[CONTENT_LENGTH];
                message.Content = new StreamContent(request.Body);
                var contentType = request.Headers[CONTENT_TYPE];
                message.Content.Headers.Add(CONTENT_LENGTH, contentLength);
                message.Content.Headers.Add(CONTENT_TYPE, contentType.ToString());
            }
        }
    }
}



