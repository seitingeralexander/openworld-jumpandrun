using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Core
{
    public abstract class Scene
    {
        protected SimContext Context { get; private set; }

        protected Scene(SimContext context)
        {
            Context = context;
        }

        public abstract void LoadContent();
        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch);
        public virtual void UnloadContent() { }
    }
}
