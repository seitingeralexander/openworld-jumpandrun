using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Core
{
    public class Camera
    {
        public Matrix Transform { get; private set; }
        public Vector2 Position { get; private set; }
        
        private Viewport _viewport;

        public Camera(Viewport viewport)
        {
            _viewport = viewport;
            Transform = Matrix.Identity;
        }

        public void Follow(Vector2 targetPosition)
        {
            Position = targetPosition;
            var offset = new Vector3(_viewport.Width / 2f, _viewport.Height / 2f, 0);
            
            Transform = Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
                        Matrix.CreateTranslation(offset);
        }
    }
}
