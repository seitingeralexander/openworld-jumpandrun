using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;

namespace JumpAndRun.Components
{
    public class TopDownController : Component
    {
        public float Speed { get; set; } = 200f;
        public Camera Camera { get; set; } // Needed for mouse position conversion

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // InputManager.Instance.Update(); // Handled by Scene/GameLoop
            
            // Mouse handling
            Vector2 mousePos = InputManager.Instance.GetMousePosition();
            Vector2 mouseWorldPos = Vector2.Transform(mousePos, Matrix.Invert(Camera.Transform));
            
            Vector2 directionToMouse = mouseWorldPos - Owner.Position;
            if (directionToMouse != Vector2.Zero) directionToMouse.Normalize();

            // Movement logic
            Vector2 forward = directionToMouse;
            Vector2 right = new Vector2(-forward.Y, forward.X);
            
            Vector2 moveDir = Vector2.Zero;
            if (InputManager.Instance.IsKeyDown(Keys.W)) moveDir += forward;
            if (InputManager.Instance.IsKeyDown(Keys.S)) moveDir -= forward;
            if (InputManager.Instance.IsKeyDown(Keys.D)) moveDir += right;
            if (InputManager.Instance.IsKeyDown(Keys.A)) moveDir -= right;

            if (moveDir != Vector2.Zero)
            {
                moveDir.Normalize();
                Owner.Position += moveDir * Speed * dt;
            }
            
            Camera.Follow(Owner.Position);
        }
    }
}
