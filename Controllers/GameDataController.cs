using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using GameAPI.Models;
using GameAPI.Data;
using Microsoft.Data.SqlClient;

namespace GameAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamedataController : ControllerBase
    {
        private readonly GameDbContext _context;

        public GamedataController(GameDbContext context)
        {
            _context = context;
        }

        [HttpGet("effects")]
        public async Task<ActionResult<IEnumerable<EffectDto>>> GetEffects()
        {
            return await _context.Effects
                .Select(e => new EffectDto
                {
                    Id = e.Effect_Id,
                    NameEn = e.Effect_Name_En,
                    NameRu = e.Effect_Name_Ru,
                    Affix = e.Effect_Affix,
                    Family = e.Effect_Family,
                    TierMin = e.Effect_Tier_Min,
                    ValueMin = e.Effect_Vmin,
                    ValueMax = e.Effect_Vmax,
                    Tag = e.Effect_Tag,
                    Chance = e.Effect_Chance
                })
                .ToListAsync();
        }

        [HttpGet("itembases")]
        public async Task<ActionResult<IEnumerable<ItemBaseDto>>> GetItemBases()
        {
            return await _context.ItemBases
                .Select(i => new ItemBaseDto
                {
                    Id = i.Item_Base_Id,
                    NameEn = i.Item_Base_Name_En,
                    NameRu = i.Item_Base_Name_Ru,
                    Family = i.Item_Base_Family,
                    Biom = i.Item_Base_Biom,
                    Stuckable = i.Item_Base_Stuckable,
                    AmountMin = i.Item_Base_Amount_Min,
                    AmountMax = i.Item_Base_Amount_Max,
                    NativeEffectId = i.Item_Base_Effect_Native,
                    WeaponSpeed = i.Item_Base_Weapon_Speed,
                    AttackRange = i.Item_Base_Attack_Range,
                    TierMin = i.Item_Base_Tier_Min,
                    TierMax = i.Item_Base_Tier_Max,
                    Slot = i.Item_Base_Slot,
                    Tag = i.Item_Base_Tag,
                    RedeEn = i.Item_Base_Rede_En,
                    RedeRu = i.Item_Base_Rede_Ru,
                    DropStrength = i.Item_Base_Drop_Strength,
                    Fbx = i.Item_Base_Fbx
                })
                .ToListAsync();
        }

        [HttpGet("charstats")]
        public async Task<ActionResult<IEnumerable<CharStatDto>>> GetCharStats()
        {
            return await _context.CharStats
                .Select(c => new CharStatDto
                {
                    Id = c.Char_Stat_Id,
                    NameEn = c.Char_Stat_Name_En,
                    NameRu = c.Char_Stat_Name_Ru,
                    RedeEn = c.Char_Stat_Rede_En,
                    RedeRu = c.Char_Stat_Rede_Ru,
                    Base = c.Char_Stat_Base,
                    Math = c.Char_Stat_Math,
                    AddTag = c.Char_Stat_Add_Tag,
                    Multi = c.Char_Stat_Multi,
                    MultiTag = c.Char_Stat_Multi_Tag
                })
                .ToListAsync();
        }
    }
}
