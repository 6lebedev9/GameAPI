﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;
using GameAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GameAPI.Data;
using Microsoft.Data.SqlClient;

namespace GameAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly GameDbContext _context;
        private readonly IConfiguration _config;

        public AccountsController(GameDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        #region SignUp
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(
            [FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(AuthResponse.ErrorResponse("Invalid data"));

            var token = await _context.Tokens
                .FirstOrDefaultAsync(t =>
                    t.TgToken == registerDto.TgToken &&
                    !t.IsUsed &&
                    t.ExpiredAt > DateTime.UtcNow);

            if (token == null)
                return BadRequest(AuthResponse.ErrorResponse("Invalid or expired token"));

            if (await _context.Accounts.AnyAsync(a => a.Email == registerDto.Email))
                return Conflict(AuthResponse.ErrorResponse("Email already exists"));

            var account = new Account
            {
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                TgId = token.TgId,
                Jwt = string.Empty,
                JwtExpiry = DateTime.UtcNow
            };

            token.IsUsed = true;
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var jwt = GenerateJwtToken(account);
            account.Jwt = jwt;
            account.JwtExpiry = DateTime.UtcNow.AddDays(1);
            await _context.SaveChangesAsync();

            return Ok(AuthResponse.SuccessResponse(account, jwt));
        }
        #endregion

        #region Auth
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginDto loginDto)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == loginDto.Email);

            if (account == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, account.PasswordHash))
            {
                var failedAttempt = await HandleFailedLogin(loginDto.Email);
                if (failedAttempt.isLocked)
                {
                    return Unauthorized(AuthResponse.ErrorResponse(
                        $"Account locked until {failedAttempt.lockedUntil}"));
                }
                return Unauthorized(AuthResponse.ErrorResponse("Invalid credentials"));
            }

            if (account.Banned)
                return Unauthorized(AuthResponse.ErrorResponse("Account banned"));

            if (account.IsLocked)
                return Unauthorized(AuthResponse.ErrorResponse(
                    $"Account locked until {account.AccountLockedUntil}"));

            account.LastLogin = DateTime.UtcNow;
            account.FailedLoginAttempts = 0;

            var jwt = GenerateJwtToken(account);
            account.Jwt = jwt;
            account.JwtExpiry = DateTime.UtcNow.AddDays(1);
            await _context.SaveChangesAsync();

            return Ok(AuthResponse.SuccessResponse(account, jwt));
        }

        private async Task<(bool isLocked, DateTime? lockedUntil)> HandleFailedLogin(string email)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == email);

            if (account == null) return (false, null);

            account.FailedLoginAttempts++;

            if (account.FailedLoginAttempts >= 5)
            {
                account.AccountLockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return (true, account.AccountLockedUntil);
            }

            await _context.SaveChangesAsync();
            return (false, null);
        }
        #endregion

        #region Updating data

        [HttpPut("update-email")]
        public async Task<ActionResult<AuthResponse>> UpdateEmail([FromBody] UpdateEmailDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var token = await _context.Tokens
                    .FirstOrDefaultAsync(t =>
                        t.TgToken == updateDto.TgToken &&
                        !t.IsUsed &&
                        t.ExpiredAt > DateTime.UtcNow);

                if (token == null)
                    return BadRequest(AuthResponse.ErrorResponse("Invalid or expired token"));

                var account = await _context.Accounts
                    .AsTracking()
                    .FirstOrDefaultAsync(a => a.TgId == token.TgId);

                if (account == null)
                    return NotFound(AuthResponse.ErrorResponse("Account not found"));

                if (await _context.Accounts.AnyAsync(a => a.Email == updateDto.NewEmail && a.AccountId != account.AccountId))
                    return Conflict(AuthResponse.ErrorResponse("Email already in use"));

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    account.Email = updateDto.NewEmail;
                    _context.Entry(account).Property(x => x.Email).IsModified = true;

                    token.IsUsed = true;
                    _context.Entry(token).Property(x => x.IsUsed).IsModified = true;

                    var jwt = GenerateJwtToken(account);
                    account.Jwt = jwt;
                    account.JwtExpiry = DateTime.UtcNow.AddDays(1);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(AuthResponse.SuccessResponse(account, jwt));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error updating email: {ex.Message}");
                    return StatusCode(500, AuthResponse.ErrorResponse("Transaction failed"));
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return StatusCode(500, AuthResponse.ErrorResponse("Database update failed"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, AuthResponse.ErrorResponse("Internal server error"));
            }
        }

        [HttpPut("update-password")]
        public async Task<ActionResult<AuthResponse>> UpdatePassword([FromBody] UpdatePasswordDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var token = await _context.Tokens
                    .FirstOrDefaultAsync(t =>
                        t.TgToken == updateDto.TgToken &&
                        !t.IsUsed &&
                        t.ExpiredAt > DateTime.UtcNow);

                if (token == null)
                    return BadRequest(AuthResponse.ErrorResponse("Invalid or expired token"));

                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.TgId == token.TgId);

                if (account == null)
                    return NotFound(AuthResponse.ErrorResponse("Account not found"));

                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.NewPassword);
                token.IsUsed = true;

                var jwt = GenerateJwtToken(account);
                account.Jwt = jwt;
                account.JwtExpiry = DateTime.UtcNow.AddDays(1);

                await _context.SaveChangesAsync();

                return Ok(AuthResponse.SuccessResponse(account, jwt));
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return StatusCode(500, AuthResponse.ErrorResponse("Database update failed"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, AuthResponse.ErrorResponse("Internal server error"));
            }
        }
        #endregion

        #region addictive methods
        private string GenerateJwtToken(Account account)
        {
            var jwtKey = _config["Jwt:Key"] ??
                throw new InvalidOperationException("JWT Key not configured");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
                new Claim("TgId", account.TgId.ToString()),
                new Claim("Role", account.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddDays(1);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured"),
                _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured"),
                claims,
                expires: expiry,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }

}