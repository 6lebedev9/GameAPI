using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class UpdatePasswordDto
    {
        [Required]
        [StringLength(5, MinimumLength = 5)]
        public string TgToken { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
