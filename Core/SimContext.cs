using JumpAndRun.World;
using JumpAndRun.Simulation;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace JumpAndRun.Core
{
    public class SimContext
    {
        private static SimContext _instance;
        public static SimContext Instance => _instance ??= new SimContext();

        public Player Player { get; private set; }
        public NPCSystem NPCSystem { get; private set; }
        public TimeSystem Time { get; private set; }
        public Town Town { get; private set; }
        public List<NPC> NPCs { get; private set; }

        private SimContext()
        {
            Player = new Player("Hero");
            Time = new TimeSystem();
            Town = new Town();
            NPCs = new List<NPC>();
            NPCSystem = new NPCSystem(this);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Time.Update(dt);
            NPCSystem.Update(gameTime);
        }
    }
}
