using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class CharacterCreateDto
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9]{3,50}$")]
        public string? CharName { get; set; }

        [Required]
        [StringLength(20)]
        public string? CharClass { get; set; }
    }
}