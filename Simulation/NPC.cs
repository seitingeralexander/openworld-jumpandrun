using Microsoft.Xna.Framework;
using JumpAndRun.World;
using System.Collections.Generic;

namespace JumpAndRun.Simulation
{
    public enum NPCState
    {
        Idle,
        Moving,
        Interacting,
        Sleeping,
        Eating
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
        
        /// <summary>
        /// Which scene the NPC is currently in (for rendering purposes).
        /// NPCs are always simulated, but only rendered in their current scene.
        /// </summary>
        public string CurrentSceneId { get; set; } = "TownScene";
        
        /// <summary>
        /// Preserved positions per scene. When NPC transitions scenes,
        /// their position in the previous scene is saved here.
        /// </summary>
        public Dictionary<string, Vector2> ScenePositions { get; set; } = new();

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
