using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Core
{
    public abstract class Scene
    {
        public abstract void LoadContent();
        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}
