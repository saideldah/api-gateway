using Microsoft.AspNetCore.Builder;

namespace ApiGateway
{
    public static class ApiGatewayRoutingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiGatewayRouting(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiGatewayRoutingMiddleware>();
        }
    }
}
