using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ApiGateway
{
    public class ApiGatewayRoutingMiddleware
    {
        #region private fields
        private readonly RequestDelegate _next; 
        #endregion
        #region ctor
        public ApiGatewayRoutingMiddleware(RequestDelegate next)
        {
            _next = next;
        } 
        #endregion
        #region public methods
        public async Task InvokeAsync(HttpContext context)
        {

            var path = await GetPath(context.Request.Path);
            var pathValues = path.Split("/");
            var serviceName = await GetServiceName(path);
            if (Helper.Applications.Keys.Any(k => k == serviceName))
            {
                var serviceUri = await GetServiceUri(serviceName, path);
                HttpResponseMessage response = await CallService(context.Request, serviceUri);
                if (response.IsSuccessStatusCode)
                {
                    context = await MapResponse(response, context);
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
        #endregion
        #region Private Methods
      
        private async Task<HttpContext> MapResponse(HttpResponseMessage response, HttpContext context)
        {
            //foreach (var header in response.Headers)
            //{
            //    string[] value;
            //    if (header.Key.ToLower().Equals("host"))
            //    {
            //        value = new string[] { context.Request.Host.ToString() };
            //    }
            //    else
            //    {
            //        value = header.Value.ToArray();
            //    }
            //    context.Response.Headers.Add(header.Key, value);
            //}
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            context.Response.ContentLength = response.Content.Headers.ContentLength;
            context.Response.StatusCode = (int)response.StatusCode;
            return context;
        }
        private async Task<HttpResponseMessage> CallService(HttpRequest request, string serviceUri)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(2)
            };
            HttpResponseMessage response;
            switch (request.Method)
            {
                case "GET":
                    response = await httpClient.GetAsync(serviceUri);
                    break;
                case "POST":
                    response = await httpClient.PostAsync(serviceUri, new StreamContent(request.Body));
                    break;
                case "PUT":
                    response = await httpClient.PutAsync(serviceUri, new StreamContent(request.Body));
                    break;
                case "DELETE":
                    response = await httpClient.DeleteAsync(serviceUri);
                    break;
                default:
                    //To Do: implement patch, head and connect
                    response = new HttpResponseMessage();
                    break;
            }
            return response;
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
            if (Helper.Applications.Keys.Any(k => k == serviceName))
            {
                var serviceUri = path.Replace($"${serviceName}", Helper.Applications[serviceName]);
                return serviceUri;
            }
            else
            {
                return null;
            }
        } 
        #endregion
    }
}
