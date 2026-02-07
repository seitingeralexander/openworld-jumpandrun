using Microsoft.Xna.Framework;
using JumpAndRun.World;

namespace JumpAndRun.Simulation
{
    public enum NPCState
    {
        Idle,
        Moving,
        Interacting,
        Sleeping
    }

    public class NPC
    {
        public string Name { get; set; }
        public Background Background { get; set; }
        public Needs Needs { get; set; }
        public Schedule Schedule { get; set; }
        public NPCState State { get; set; }
        public Vector2 Position { get; set; }
        public string CurrentLocationId { get; set; }

        public NPC(string name, Background background)
        {
            Name = name;
            Background = background;
            Needs = new Needs();
            Schedule = new Schedule();
            State = NPCState.Idle;
        }
    }
}
