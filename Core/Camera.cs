using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Core
{
    public class Camera
    {
        public Matrix Transform { get; private set; }
        public Vector2 Position { get; private set; }
        
        /// <summary>
        /// Current zoom level. 1.0 = default, >1.0 = zoomed in, <1.0 = zoomed out
        /// </summary>
        public float Zoom { get; private set; } = 1.0f;
        
        /// <summary>
        /// Minimum zoom level (zoomed out)
        /// </summary>
        public float MinZoom { get; set; } = 0.25f;
        
        /// <summary>
        /// Maximum zoom level (zoomed in)
        /// </summary>
        public float MaxZoom { get; set; } = 4.0f;
        
        /// <summary>
        /// Speed of zoom change per key press/scroll step
        /// </summary>
        public float ZoomSpeed { get; set; } = 0.1f;
        
        private Viewport _viewport;

        public Camera(Viewport viewport)
        {
            _viewport = viewport;
            Transform = Matrix.Identity;
        }

        /// <summary>
        /// Zoom in (increase zoom level)
        /// </summary>
        public void ZoomIn()
        {
            Zoom = MathHelper.Clamp(Zoom + ZoomSpeed, MinZoom, MaxZoom);
        }

        /// <summary>
        /// Zoom out (decrease zoom level)
        /// </summary>
        public void ZoomOut()
        {
            Zoom = MathHelper.Clamp(Zoom - ZoomSpeed, MinZoom, MaxZoom);
        }

        /// <summary>
        /// Set the zoom level directly
        /// </summary>
        public void SetZoom(float zoom)
        {
            Zoom = MathHelper.Clamp(zoom, MinZoom, MaxZoom);
        }

        /// <summary>
        /// Reset zoom to default (1.0)
        /// </summary>
        public void ResetZoom()
        {
            Zoom = 1.0f;
        }

        public void Follow(Vector2 targetPosition)
        {
            Position = targetPosition;
            var offset = new Vector3(_viewport.Width / 2f, _viewport.Height / 2f, 0);
            
            // Apply translation first, then scale around the center of the viewport
            Transform = Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
                        Matrix.CreateScale(Zoom, Zoom, 1f) *
                        Matrix.CreateTranslation(offset);
        }
    }
}
