namespace GameAPI.Models
{
    public class CharStatDto
    {
        public int Id { get; set; }
        public string? NameEn { get; set; }
        public string? NameRu { get; set; }
        public string? RedeEn { get; set; }
        public string? RedeRu { get; set; }
        public int Base { get; set; }
        public string? Math { get; set; }
        public string? AddTag { get; set; }
        public string? Multi { get; set; }
        public string? MultiTag { get; set; }
    }

    public class CharStat
    {
        public int Char_Stat_Id { get; set; }
        public string? Char_Stat_Name_En { get; set; }
        public string? Char_Stat_Name_Ru { get; set; }
        public string? Char_Stat_Rede_En { get; set; }
        public string? Char_Stat_Rede_Ru { get; set; }
        public int Char_Stat_Base { get; set; }
        public string? Char_Stat_Math { get; set; }
        public string? Char_Stat_Add_Tag { get; set; }
        public string? Char_Stat_Multi { get; set; }
        public string? Char_Stat_Multi_Tag { get; set; }
    }

}
