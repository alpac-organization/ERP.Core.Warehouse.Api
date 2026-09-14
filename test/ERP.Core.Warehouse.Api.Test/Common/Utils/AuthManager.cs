using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ERP.Core.Warehouse.Api.Test.Common.Utils
{
    public static class AuthManager
    {
        public static string GenerateJwtToken(string jwtKey, Guid userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);            

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new("user_id",   userId.ToString()),
                new("username", "test.user"),
                new("fullname", "Test User"),
            };

            var token = new JwtSecurityToken(
                issuer: "ERP.Core.Wareouse.Api",
                audience: "ERP.Clients.Web",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    } 
}