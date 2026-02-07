using JumpAndRun.World;
using System.Collections.Generic;

namespace JumpAndRun.Core
{
    public class SimContext
    {
        public TimeSystem Time { get; private set; }
        public Town Town { get; private set; }
        public List<JumpAndRun.Simulation.NPC> NPCs { get; private set; }

        public SimContext()
        {
            Time = new TimeSystem();
            Town = new Town();
            NPCs = new List<JumpAndRun.Simulation.NPC>();
        }
    }
}
