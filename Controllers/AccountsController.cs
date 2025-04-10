using Microsoft.AspNetCore.Mvc;
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

        [Authorize]
        [HttpPut("update-email")]
        public async Task<ActionResult<AuthResponse>> UpdateEmail(
    [FromBody] UpdateEmailDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (!long.TryParse(User.FindFirst("TgId")?.Value, out var tgId))
                    return Unauthorized(AuthResponse.ErrorResponse("Invalid TgId in token"));

                var token = await _context.Tokens
                    .FirstOrDefaultAsync(t =>
                        t.TgToken == updateDto.TgToken &&
                        t.TgId == tgId &&
                        !t.IsUsed &&
                        t.ExpiredAt > DateTime.UtcNow);

                if (token == null)
                    return BadRequest(AuthResponse.ErrorResponse("Invalid or expired token"));

                var accountId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var account = await _context.Accounts
                    .AsTracking()
                    .FirstOrDefaultAsync(a => a.AccountId == accountId);

                if (account == null)
                    return NotFound(AuthResponse.ErrorResponse("Account not found"));

                if (await _context.Accounts.AnyAsync(a => a.Email == updateDto.NewEmail && a.AccountId != accountId))
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

        [Authorize]
        [HttpPut("update-password")]
        public async Task<ActionResult<AuthResponse>> UpdatePassword(
            [FromBody] UpdatePasswordDto updateDto)
        {
            var tgIdClaim = User.FindFirstValue("TgId") ??
                throw new InvalidOperationException("TgId claim not found");

            if (!long.TryParse(tgIdClaim, out var tgId))
                return Unauthorized(AuthResponse.ErrorResponse("Invalid TgId in token"));

            var token = await _context.Tokens
                .FirstOrDefaultAsync(t =>
                    t.TgToken == updateDto.TgToken &&
                    t.TgId == tgId &&
                    !t.IsUsed &&
                    t.ExpiredAt > DateTime.UtcNow);

            if (token == null)
                return BadRequest(AuthResponse.ErrorResponse("Invalid or expired token"));

            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                throw new InvalidOperationException("NameIdentifier claim not found");

            if (!int.TryParse(accountIdClaim, out var accountId))
                return Unauthorized(AuthResponse.ErrorResponse("Invalid account ID"));

            var account = await _context.Accounts.FindAsync(accountId);
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
        #endregion

        [HttpPost("update-email-manual")]
        public IActionResult UpdateEmailManual([FromQuery] int accountId, [FromQuery] string newEmail)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using var command = new SqlCommand(
                    "UPDATE Accounts SET Email = @email WHERE AccountId = @id",
                    connection);

                command.Parameters.AddWithValue("@id", accountId);
                command.Parameters.AddWithValue("@email", newEmail);

                int rowsAffected = command.ExecuteNonQuery();

                return Ok(new
                {
                    Success = rowsAffected > 0,
                    Message = $"Email updated in {rowsAffected} row(s)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Details = ex.InnerException?.Message
                });
            }
        }

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