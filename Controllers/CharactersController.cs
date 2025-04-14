using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using GameAPI.Models;
using GameAPI.Data;

namespace GameAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharactersController : ControllerBase
    {
        private readonly GameDbContext _context;

        public CharactersController(GameDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Character>>> GetCharacters()
        {
            var accountId = GetCurrentAccountId();
            return await _context.Characters
                .Where(c => c.AccountId == accountId && !c.IsDeleted)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Character>> GetCharacter(int id)
        {
            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.CharId == id && !c.IsDeleted);

            if (character == null || character.AccountId != GetCurrentAccountId())
                return NotFound();

            return character;
        }

        [HttpPost]
        public async Task<ActionResult<Character>> CreateCharacter(CharacterCreateDto dto)
        {
            var accountId = GetCurrentAccountId();
            var account = await _context.Accounts
                .Include(a => a.Characters)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null)
                return Unauthorized();

            if (account.Characters.Count(c => !c.IsDeleted) >= account.MaxCharCount)
                return BadRequest(new { Message = $"Maximum character limit reached ({account.MaxCharCount})" });

            if (!new[] { "Warrior", "Mage", "Rogue" }.Contains(dto.CharClass))
                return BadRequest(new { Message = "Invalid character class" });

            var character = new Character
            {
                AccountId = accountId,
                CharName = dto.CharName,
                CharClass = dto.CharClass,
                Exp = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCharacter), new { id = character.CharId }, character);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCharacter(int id, CharacterUpdateDto dto)
        {
            var character = await _context.Characters
                .FirstOrDefaultAsync(c => c.CharId == id && !c.IsDeleted);

            if (character == null || character.AccountId != GetCurrentAccountId())
                return NotFound();

            character.CharInfo = dto.CharInfo;
            character.Skills = dto.Skills;
            character.Inventory = dto.Inventory;
            character.Stash = dto.Stash;
            character.QuestInfo = dto.QuestInfo;
            character.ChatHistory = dto.ChatHistory;
            character.LastPlayed = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCharacter(int id)
        {
            var character = await _context.Characters.FindAsync(id);

            if (character == null || character.AccountId != GetCurrentAccountId())
                return NotFound();

            character.IsDeleted = true;
            character.DeleteDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdClaim))
            {
                throw new UnauthorizedAccessException("Invalid account ID claim");
            }
            return int.Parse(accountIdClaim);
        }
    }
}