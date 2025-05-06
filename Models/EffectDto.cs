namespace GameAPI.Models
{
    public class EffectDto
    {
        public int Id { get; set; }
        public string? NameEn { get; set; }
        public string? NameRu { get; set; }
        public string? Affix { get; set; }
        public string? Family { get; set; }
        public int TierMin { get; set; }
        public int ValueMin { get; set; }
        public int ValueMax { get; set; }
        public string? Tag { get; set; }
        public int Chance { get; set; }
    }

    public class Effect
    {
        public int Effect_Id { get; set; }
        public string? Effect_Name_En { get; set; }
        public string? Effect_Name_Ru { get; set; }
        public string? Effect_Affix { get; set; }
        public string? Effect_Family { get; set; }
        public int Effect_Tier_Min { get; set; }
        public int Effect_Vmin { get; set; }
        public int Effect_Vmax { get; set; }
        public string? Effect_Tag { get; set; }
        public int Effect_Chance { get; set; }
    }
}
