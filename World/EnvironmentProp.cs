using Microsoft.Xna.Framework;

namespace JumpAndRun.World
{
    public enum PropType
    {
        Tree,
        PineTree,
        Rock,
        Cactus,
        Bush
    }

    public class EnvironmentProp
    {
        public Vector2 Position { get; set; }
        public PropType Type { get; set; }
        public float Size { get; set; } // Scale modifier
        
        public EnvironmentProp(Vector2 position, PropType type, float size = 1f)
        {
            Position = position;
            Type = type;
            Size = size;
        }
    }
}
