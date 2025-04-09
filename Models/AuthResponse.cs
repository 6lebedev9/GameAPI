namespace GameAPI.Models
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "Authentication successful";
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public long TgId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public DateTime TokenExpiry { get; set; }
        public int Role { get; set; }
        public bool IsBanned { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedUntil { get; set; }
        public int MCoins { get; set; }
        public int MaxCharCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public static AuthResponse SuccessResponse(Account account, string token)
        {
            return new AuthResponse
            {
                Success = true,
                AccountId = account.AccountId,
                Email = account.Email,
                TgId = account.TgId,
                AccessToken = token,
                TokenExpiry = account.JwtExpiry,
                Role = account.Role,
                IsBanned = account.Banned,
                IsLocked = account.AccountLockedUntil.HasValue &&
                          account.AccountLockedUntil > DateTime.UtcNow,
                LockedUntil = account.AccountLockedUntil,
                MCoins = account.MCoins,
                MaxCharCount = account.MaxCharCount,
                CreatedAt = account.CreatedAt,
                LastLogin = account.LastLogin
            };
        }
        public static AuthResponse ErrorResponse(string message)
        {
            return new AuthResponse
            {
                Success = false,
                Message = message
            };
        }
    }
}