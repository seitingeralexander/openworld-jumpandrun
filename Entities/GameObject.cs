using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using JumpAndRun.Components;
using System.Linq;

namespace JumpAndRun.Entities
{
    public class GameObject
    {
        public Vector2 Position { get; set; }
        
        // Components
        private List<Component> _components = new List<Component>();

        public void AddComponent(Component component)
        {
            component.Owner = this;
            _components.Add(component);
            component.Start();
        }

        public T GetComponent<T>() where T : Component
        {
            return _components.OfType<T>().FirstOrDefault();
        }

        public virtual void Update(GameTime gameTime)
        {
            foreach (var component in _components)
            {
                component.Update(gameTime);
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
             foreach (var component in _components)
            {
                component.Draw(spriteBatch);
            }
        }
    }
}
