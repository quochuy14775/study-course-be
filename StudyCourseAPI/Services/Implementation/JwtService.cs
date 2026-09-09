﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var jwt  = _config.GetSection("Jwt");
            var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name,  user.UserName ?? ""),
                new(ClaimTypes.Email, user.Email    ?? ""),
            };

            // 🔥 QUAN TRỌNG: Dùng ClaimTypes.Role (standard) thay vì "role"
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(int.Parse(jwt["TokenLifetimeMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}