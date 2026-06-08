using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using ERP.Core.Manager.Api.Infrastructure.Attributes;
using Microsoft.AspNetCore.Http; 

namespace ERP.Core.Manager.Api.Infrastructure.Middlewares
{
    public class AuthMiddleware(RequestDelegate _next) 
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var hasTokenAttribute = endpoint?.Metadata.GetMetadata<HasTokenAttribute>();

            if (hasTokenAttribute == null)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);

                        if (jwtToken.ValidTo < DateTime.UtcNow)
                        {
                            await ReturnError(context, "Token_Expired", "La sesión ha expirado.");
                            return;
                        }

                        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value 
                                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;

                        context.Items["UserId"] = userId;
                    }
                }
                catch (Exception)
                {
                    await ReturnError(context, "Invalid_Token", "El token proporcionado no es válido.");
                    return;
                }
            }
            else
            {
                await ReturnError(context, "Missing_Token", "Se requiere un token para acceder a este recurso.");
                return;
            }

            await _next(context);
        }

        private static async Task ReturnError(HttpContext context, string typeError, string description)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            
            // Formato exacto al que pediste
            var response = new { 
                Status = 401,
                Error = new { 
                    TypeError = typeError, 
                    Description = description 
                },
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }   
}