using Microsoft.EntityFrameworkCore;
using TeamService.Models;

namespace TeamService.DBContexts
{
    public class TeamServiceContext : DbContext
    {
        public TeamServiceContext(DbContextOptions<TeamServiceContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
    }
}
