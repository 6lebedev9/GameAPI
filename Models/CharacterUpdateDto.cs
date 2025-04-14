namespace GameAPI.Models
{
    public class CharacterUpdateDto
    {
        public string? CharInfo { get; set; }
        public string? Skills { get; set; }
        public string? Inventory { get; set; }
        public string? Stash { get; set; }
        public string? QuestInfo { get; set; }
        public string? ChatHistory { get; set; }
    }
}