using Microsoft.Xna.Framework;

namespace JumpAndRun.Components
{
    public class BoxCollider : Component
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Vector2 Offset { get; set; }

        public BoxCollider(int width, int height)
        {
            Width = width;
            Height = height;
            Offset = new Vector2(-width / 2f, -height / 2f); // Default to centered
        }

        public Rectangle Bounds
        {
            get
            {
                if (Owner == null) return Rectangle.Empty;
                return new Rectangle(
                    (int)(Owner.Position.X + Offset.X),
                    (int)(Owner.Position.Y + Offset.Y),
                    Width,
                    Height
                );
            }
        }
    }
}
