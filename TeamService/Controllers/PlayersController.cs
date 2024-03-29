﻿using System;
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
    public class PlayersController : ControllerBase
    {
        private readonly TeamServiceContext _context;

        public PlayersController(TeamServiceContext context)
        {
            _context = context;
        }

        // GET: api/Players
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Player>>> GetPlayers([FromQuery] string? lastName, [FromQuery] int page = 1, [FromQuery] int itemsPerPage = 10)
        {
            if (_context.Players == null)
            {
                return NotFound();
            }

            //return await _context.Players.ToListAsync();
            return await GetPaginatedPlayers(lastName, ref page, ref itemsPerPage);
        }

        // GET: api/Players/5
        [HttpGet("{id:int:min(0)}")]
        public async Task<ActionResult<Player>> GetPlayer(int id)
        {
            if (_context.Players == null)
            {
                return NotFound();
            }
            var player = await _context.Players.FindAsync(id);

            if (player == null)
            {
                return NotFound();
            }

            return player;
        }

        // PUT: api/Players/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int:min(0)}")]
        public async Task<IActionResult> PutPlayer(int id, Player player)
        {
            if (id != player.Id || InvalidPlayer(player)) return BadRequest();

            _context.Entry(player).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlayerExists(id))
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

        // POST: api/Players
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Player>> PostPlayer(Player player)
        {
            if (_context.Players == null)
            {
                return Problem("Entity set 'TeamServiceContext.Players'  is null.");
            }

            if (InvalidPlayer(player)) return BadRequest();

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }

        // DELETE: api/Players/5
        [HttpDelete("{id:int:min(0)}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            if (_context.Players == null)
            {
                return NotFound();
            }
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlayerExists(int id)
        {
            return (_context.Players?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        // Name and Location are required.
        private bool InvalidPlayer(Player player)
        {
            return player.FirstName.IsNullOrEmpty() || player.LastName.IsNullOrEmpty();
        }

        private Task<List<Player>> GetPaginatedPlayers(string lastName, ref int page, ref int itemsPerPage)
        {
            // quick and dirty pagination. Specs say "all teams" but pagination should be implemented here.
            page = (page < 1 ? 1 : page);
            itemsPerPage = (itemsPerPage < 10 ? 10 : itemsPerPage);
            itemsPerPage = (itemsPerPage > 100 ? 100 : itemsPerPage);
            var start = (page - 1) * itemsPerPage;

            if (lastName.IsNullOrEmpty()) return _context.Players.Skip(start).Take(itemsPerPage).ToListAsync();
            return _context.Players.Where(p => p.LastName == lastName).Skip(start).Take(itemsPerPage).ToListAsync();
        }
    }
}
