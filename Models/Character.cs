using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class Character
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CharId { get; set; }

        [Required]
        public int AccountId { get; set; }

        [ForeignKey("AccountId")]
        public Account? Account { get; set; }

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9]{3,}$")]
        public string? CharName { get; set; }

        [Required]
        [MaxLength(20)]
        public string? CharClass { get; set; }

        [Required]
        [Range(0, long.MaxValue)]
        public long Exp { get; set; } = 0;

        public string? Skills { get; set; }
        public string? CharInfo { get; set; }
        public string? Inventory { get; set; }
        public string? Stash { get; set; }
        public string? QuestInfo { get; set; }
        public string? ChatHistory { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastPlayed { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeleteDate { get; set; }
    }
}