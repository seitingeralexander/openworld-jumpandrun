using JumpAndRun.World;
using System.Collections.Generic;

namespace JumpAndRun.Simulation
{
    public class Background
    {
        public string Job { get; set; }
        public string Personality { get; set; } // Could be an Enum or complex object later
        public string HomeLocationId { get; set; }

        public Background(string job, string personality, string homeId)
        {
            Job = job;
            Personality = personality;
            HomeLocationId = homeId;
        }
    }
}
