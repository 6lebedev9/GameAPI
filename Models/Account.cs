using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameAPI.Models
{
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        public long TgId { get; set; }
        [Required]
        public int Role { get; set; } = 1;
        [Required]
        public bool Banned { get; set; } = false;
        [Required]
        public string Jwt { get; set; } = string.Empty;
        [Required]
        public DateTime JwtExpiry { get; set; }
        [Required]
        [Range(0, int.MaxValue)]
        public int MCoins { get; set; } = 0;
        [Required]
        [Range(1, 10)]
        public int MaxCharCount { get; set; } = 2;
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        [Range(0, 10)]
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? AccountLockedUntil { get; set; }

        public bool IsLocked => AccountLockedUntil.HasValue && AccountLockedUntil > DateTime.UtcNow;

        public bool IsJwtValid => JwtExpiry > DateTime.UtcNow;

        public bool IsAdmin => Role >= 10;

        public bool IsModerator => Role >= 2;
    }
}