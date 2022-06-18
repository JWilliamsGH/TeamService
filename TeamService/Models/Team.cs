using System.Collections.ObjectModel;

namespace TeamService.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public List<Player>? Players { get; set; }
    }
}
