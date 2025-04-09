using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class UpdateEmailDto
    {
        [Required]
        [StringLength(5, MinimumLength = 5)]
        public string TgToken { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(50)]
        public string NewEmail { get; set; } = string.Empty;
    }
}
