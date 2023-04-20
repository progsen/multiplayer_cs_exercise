using GameShared;
using Microsoft.AspNetCore.Mvc;

namespace MultiPlayer.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {

        static List<Player> players = new List<Player>();

        public GameController()
        {
        }

        [HttpGet]
        public IEnumerable<Player> Get()
        {
            return players;
        }

        [HttpPost]
        public void Post([FromBody] Player player)
        {
            Player known = players.FirstOrDefault(i=>i.id == player.id);
            if(known ==null)
            {
                players.Add(player);
            }
            else
            {
                known.x= player.x;
                known.y = player.y;
            }
        }
    }
}