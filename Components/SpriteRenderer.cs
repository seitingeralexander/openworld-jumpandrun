using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Components
{
    public class SpriteRenderer : Component
    {
        public Texture2D Texture { get; set; }
        public Color Color { get; set; } = Color.White;
        public Vector2 Origin { get; set; }

        public SpriteRenderer(Texture2D texture)
        {
            Texture = texture;
            Origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null)
            {
               spriteBatch.Draw(Texture, Owner.Position, null, Color, 0f, Origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
