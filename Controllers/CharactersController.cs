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
        public async Task<ActionResult<CharacterResponseDto>> CreateCharacter([FromBody] CharacterCreateDto dto)
        {
            var accountId = GetCurrentAccountId();
            var account = await _context.Accounts
                .Include(a => a.Characters)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null)
                return Unauthorized();

            if (account.Characters.Count >= account.MaxCharCount)
                return BadRequest("Maximum character limit reached");

            bool nameExists = await _context.Characters
                .AnyAsync(c => c.AccountId == accountId &&
                              c.CharName.ToLower() == dto.CharName.ToLower() &&
                              !c.IsDeleted);

            if (nameExists)
            {
                return Conflict(new ErrorResponse
                {
                    Message = $"Character name '{dto.CharName}' already exists"
                });
            }

            var character = new Character
            {
                AccountId = accountId,
                CharName = dto.CharName,
                CharClass = dto.CharClass,
                Exp = 0,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Characters.Add(character);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Conflict(new ErrorResponse
                {
                    Message = $"Character name '{dto.CharName}' already exists"
                });
            }

            return Ok(new CharacterResponseDto
            {
                CharId = character.CharId,
                AccountId = character.AccountId,
                CharName = character.CharName,
                CharClass = character.CharClass,
                Exp = character.Exp,
                CreatedAt = character.CreatedAt
            });
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx &&
                   (sqlEx.Number == 2601 || sqlEx.Number == 2627);
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