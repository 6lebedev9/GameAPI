using GameAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameAPI.Data;

namespace GameAPI.Services
{
    public class AccountService
    {
        private readonly IConfiguration _configuration;
        private readonly GameDbContext _context;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IConfiguration configuration,
            GameDbContext context,
            ILogger<AccountService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public string GenerateJwtToken(Account account)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(
                jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim("TgId", account.TgId.ToString()),
                new Claim("Role", account.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            _logger.LogInformation($"Generated JWT for account {account.AccountId}");
            return tokenHandler.WriteToken(token);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password verification failed");
                return false;
            }
        }

        public string GenerateToken(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[length];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public async Task<bool> ValidateTelegramToken(string token, long tgId)
        {
            var dbToken = await _context.Tokens
                .FirstOrDefaultAsync(t =>
                    t.TgToken == token &&
                    t.TgId == tgId &&
                    !t.IsUsed &&
                    t.ExpiredAt > DateTime.UtcNow);

            return dbToken != null;
        }
    }
}