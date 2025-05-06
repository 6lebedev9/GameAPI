namespace GameAPI.Models
{
    public class ItemBaseDto
    {
        public int Id { get; set; }
        public string? NameEn { get; set; }
        public string? NameRu { get; set; }
        public string? Family { get; set; }
        public string? Biom { get; set; }
        public string? Stuckable { get; set; }
        public int AmountMin { get; set; }
        public int AmountMax { get; set; }
        public int? NativeEffectId { get; set; }
        public double? WeaponSpeed { get; set; }
        public double? AttackRange { get; set; }
        public int TierMin { get; set; }
        public int TierMax { get; set; }
        public string? Slot { get; set; }
        public string? Tag { get; set; }
        public string? RedeEn { get; set; }
        public string? RedeRu { get; set; }
        public int DropStrength { get; set; }
        public string? Fbx { get; set; }
    }

    public class ItemBase
    {
        public int Item_Base_Id { get; set; }
        public string? Item_Base_Name_En { get; set; }
        public string? Item_Base_Name_Ru { get; set; }
        public string? Item_Base_Family { get; set; }
        public string? Item_Base_Biom { get; set; }
        public string? Item_Base_Stuckable { get; set; }
        public int Item_Base_Amount_Min { get; set; }
        public int Item_Base_Amount_Max { get; set; }
        public int? Item_Base_Effect_Native { get; set; }
        public double? Item_Base_Weapon_Speed { get; set; }
        public double? Item_Base_Attack_Range { get; set; }
        public int Item_Base_Tier_Min { get; set; }
        public int Item_Base_Tier_Max { get; set; }
        public string? Item_Base_Slot { get; set; }
        public string? Item_Base_Tag { get; set; }
        public string? Item_Base_Rede_En { get; set; }
        public string? Item_Base_Rede_Ru { get; set; }
        public int Item_Base_Drop_Strength { get; set; }
        public string? Item_Base_Fbx { get; set; }
    }
}
