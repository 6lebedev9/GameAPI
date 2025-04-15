using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameAPI.Models
{
    public class Token
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long TgId { get; set; }

        public long ChatId { get; set; }

        [Required]
        [StringLength(5)]
        public string TgToken { get; set; } = string.Empty;

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; } = false;
    }
}