using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Infrastructure.Identity
{
    internal sealed class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
            => _configuration = configuration;

        public string GenerateAccessToken(User user)
        {
            var key = GetSigningKey();
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(
                _configuration.GetValue<int>("Jwt:ExpiryHours"));

            var claims = BuildClaims(user);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiry,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }
        private SymmetricSecurityKey GetSigningKey()
        {
            var secretKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is not configured in appsettings.json.");

            if (secretKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key must be at least 32 characters for HMAC-SHA256.");

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        }

        private static IEnumerable<Claim> BuildClaims(User user) =>
        [
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
        new Claim(ClaimTypes.Email,              user.Email),
        new Claim(ClaimTypes.Name,               user.Name),
        new Claim(ClaimTypes.Role,               user.Role.ToString()),
    ];
    
       public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = GetSigningKey(),
                ClockSkew = TimeSpan.Zero
            };

            var handler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;

            try
            {
                principal = handler.ValidateToken(token, parameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new UnauthorizedException("Invalid access token algorithm.");
                }
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new UnauthorizedException("Invalid access token.");
            }

            return principal;
        }
    }
}