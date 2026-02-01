using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JumpAndRun.Entities;

namespace JumpAndRun.Components
{
    public abstract class Component
    {
        public GameObject Owner { get; set; }

        public virtual void Start() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
    }
}
