using Microsoft.Xna.Framework;

namespace JumpAndRun.Simulation
{
    /// <summary>
    /// Core player data that persists across scenes.
    /// Contains stats, inventory, equipment, and position.
    /// </summary>
    public class Player
    {
        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public PlayerStats Stats { get; private set; }
        public Inventory Inventory { get; private set; }
        public Equipment Equipment { get; private set; }

        public Player(string name)
        {
            Name = name;
            Position = new Vector2(50, 50); // Default spawn position
            Stats = new PlayerStats();
            Inventory = new Inventory();
            Equipment = new Equipment();
        }
    }
}
