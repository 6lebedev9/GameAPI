using System.ComponentModel.DataAnnotations;

public class CharacterCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3-50 chars")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers allowed")]
    public string? CharName { get; set; }

    [Required]
    [RegularExpression("^(Warrior|Mage|Rogue)$", ErrorMessage = "Invalid class")]
    public string? CharClass { get; set; }
}