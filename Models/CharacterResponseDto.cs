namespace GameAPI.Models
{
    public class CharacterResponseDto
    {
        public int CharId { get; set; }
        public int AccountId { get; set; } 
        public string? CharName { get; set; }
        public string? CharClass { get; set; }
        public long Exp { get; set; }
        public string? Skills { get; set; }
        public string? CharInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPlayed { get; set; }
    }
}