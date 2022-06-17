using System;
using System.Collections.Generic;
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
            var team = await _context.Teams.FindAsync(id);

            if (team == null)
            {
                return NotFound();
            }

            return team.Players ?? new List<Player>();
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

        // No two Teams should exist with the same Name and Location
        // In a real app I would go back to the creater of the story for clarification.
        // Does this mean that no duplication is allowed?
        // Does this mean that a given location can not have duplicate names but a different
        // location could have a team name that is the same as another location?
        // For the purposes of this, I will assume no duplications are allowed.
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
