using System;
using System.ComponentModel.DataAnnotations;

namespace GameAPI.Models
{
    public class Token
    {
        public long TgId { get; set; }
        public long ChatId { get; set; }
        public string TgToken { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}