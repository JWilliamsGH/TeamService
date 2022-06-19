using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamService.DBContexts;
using TeamService.Models;
using TeamService.Utilities;

namespace TeamService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly TeamServiceContext _context;
        private const int MaxPlayers = 8;

        public TeamsController(TeamServiceContext context)
        {
            _context = context;
        }

        // GET: api/Teams
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Team>>> GetTeams([FromQuery] string? sortOrder, [FromQuery] int page = 1, [FromQuery] int itemsPerPage = 10)
        {
            if (_context.Teams == null)
            {
                return NotFound();
            }

            return await GetTeams(sortOrder, ref page, ref itemsPerPage);
        }

        // GET: api/Teams/5
        [HttpGet("{id:int:min(0)}")]
        public async Task<ActionResult<Team>> GetTeam(int id)
        {
            if (_context.Teams == null)
            {
                return NotFound();
            }
            var team = await _context.Teams.FindAsync(id);

            if (team == null)
            {
                return NotFound();
            }

            return team;
        }

        // GET: api/Teams/5/Players
        [HttpGet("{id:int:min(0)}/Players")]
        public async Task<ActionResult<List<Player>>> GetPlayersOnTeam(int id)
        {
            if (_context.Teams == null)
            {
                return NotFound();
            }

            var team = _context.Teams.Include(t => t.Players).FirstOrDefault(t => t.Id == id);

            if (team == null)
            {
                return NotFound();
            }

            return team.Players ?? new List<Player>();
        }

        // POST: api/Teams
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Team>> PostTeam(TeamDTO teamDTO)
        {
            var team = CreateFromDTO(teamDTO);

            if (_context.Teams == null) return Problem("Entity set 'TeamServiceContext.Teams'  is null.");

            if (InvalidTeam(team)) return BadRequest();
            if (DuplicateTeamExists(team.Name, team.Location)) return Conflict(); // No duplicates

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
        }

        // PUT: api/Teams/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int:min(0)}")]
        public async Task<IActionResult> PutTeam(int id, TeamDTO teamDTO)
        {
            var team = CreateFromDTO(teamDTO);

            if (id != team.Id || InvalidTeam(team)) return BadRequest();
            if (DuplicateTeamExists(team)) return Conflict();

            _context.Entry(team).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeamExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Teams/5/Players/2
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int:min(0)}/Players/{playerId:int:min(0)}")]
        public async Task<ActionResult<Team>> PutPlayerById(int id, int playerId)
        {
            if (_context.Teams == null) return Problem("Entity set 'TeamServiceContext.Teams'  is null.");
            
            // Make sure both IDs are valid so we can relativly safely assume team and player are not null
            if (!TeamExists(id) || !PlayerExists(playerId)) return NotFound("Either the Team or Player does not exist.");

            // If the team is at capacity we can exit without any other checks
            var team = _context.Teams.Include(t => t.Players).FirstOrDefault(t => t.Id == id);
            if (team?.Players != null && (team.Players.Count) >= MaxPlayers) return BadRequest("Player count exceeded.");

            // If the player exists on the team we can exit
            var player = _context.Players.FirstOrDefault(p => p.Id == playerId);
            if (_context.Teams.Any(t => t.Players.Contains(player))) return BadRequest("Player already a member of another team.");

            // Add the player
            team?.Players.Add(player);
            _context.Entry(team).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeamExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Teams/5
        [HttpDelete("{id:int:min(0)}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            if (_context.Teams == null)
            {
                return NotFound();
            }
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Remove a player from a team
        // DELETE: api/Teams/5/Players/5
        [HttpDelete("{id:int:min(0)}/Players/{playerId:int:min(0)}")]
        public async Task<IActionResult> DeletePlayerFromTeam(int id, int playerId)
        {
            if (_context.Teams == null) return NotFound();

            // Make sure both IDs are valid so we can relativly safely assume team and player are not null
            if (!TeamExists(id) || !PlayerExists(playerId)) return NotFound("Either the Team or Player does not exist.");

            var team = _context.Teams.Include(t => t.Players).FirstOrDefault(t => t.Id == id);
            var player = _context.Players.FirstOrDefault(p => p.Id == playerId);

            // Add the player
            team?.Players.Remove(player);
            _context.Entry(team).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private Team CreateFromDTO(TeamDTO teamDTO)
        {
            return new Team()
            {
                Id = teamDTO.Id,
                Name = teamDTO.Name,
                Location = teamDTO.Location
            };
        }

        private bool TeamExists(int id)
        {
            return (_context.Teams?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private bool PlayerExists(int id)
        {
            return (_context.Players?.Any(p => p.Id == id)).GetValueOrDefault();
        }

        // No two Teams should exist with the same Name and Location
        // In a real app I would go back to the creater of the story for clarification.
        // Does this mean that no duplication is allowed?
        // Does this mean that a given location can not have duplicate names but a different
        // location could have a team name that is the same as another location?
        // Is Team A at Location 1 and Team B at Location 1 valid?
        // Is Team A at Location 1 and Team A at Location 2 valid?
        // Or is it just Team A at Location 1 and Team B at Location 2 that's valid?
        // For the purposes of this, I will assume no duplications are allowed and go with the last example
        // Honestly I would do this at the DB level with a unique constraint rather than in code.
        private bool DuplicateTeamExists(string? name, string? location)
        {
            return (_context.Teams?.Any(x =>
            x.Name.NullSafeToLowerInvariant() == name.NullSafeToLowerInvariant()
            || x.Location.NullSafeToLowerInvariant() == location.NullSafeToLowerInvariant()
            )).GetValueOrDefault();
        }
        private bool DuplicateTeamExists(Team team)
        {
            return (_context.Teams?.Any(x =>
            x.Id != team.Id
            && (x.Name.NullSafeToLowerInvariant() == team.Name.NullSafeToLowerInvariant()
            || x.Location.NullSafeToLowerInvariant() == team.Location.NullSafeToLowerInvariant())
            )).GetValueOrDefault();
        }

        // Name and Location are required.
        private bool InvalidTeam(Team team)
        {
            return team.Name.IsNullOrEmpty() || team.Location.IsNullOrEmpty();
        }

        private Task<List<Team>> GetTeams(string? sortOrder, ref int page, ref int itemsPerPage)
        {
            // quick and dirty pagination. Specs say "all teams" but pagination should be implemented here.
            page = (page < 1 ? 1 : page);
            itemsPerPage = (itemsPerPage < 10 ? 10 : itemsPerPage);
            itemsPerPage = (itemsPerPage > 100 ? 100 : itemsPerPage);
            var start = (page - 1) * itemsPerPage;

            return GetSortedTeams(sortOrder).Skip(start).Take(itemsPerPage).ToListAsync();
        }

        private IQueryable<Team> GetSortedTeams(string? sortOrder)
        {
            var teams = from t in _context.Teams select t;
            switch (sortOrder.NullSafeToLowerInvariant())
            {
                case "name":
                    teams = teams.OrderBy(t => t.Name);
                    break;
                case "name_desc":
                    teams = teams.OrderByDescending(t => t.Name);
                    break;
                case "location":
                    teams = teams.OrderBy(t => t.Location);
                    break;
                case "location_desc":
                    teams = teams.OrderByDescending(t => t.Location);
                    break;
                default:
                    break;
            }

            return teams;
        }
    }
}
