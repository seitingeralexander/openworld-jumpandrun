using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.Entities;
using JumpAndRun.Simulation;
using System.Collections.Generic;

namespace JumpAndRun.Components
{
    public class SideScrollController : Component
    {
        public float MoveSpeed { get; set; } = 200f;
        public float JumpStrength { get; set; } = -500f;
        public float Gravity { get; set; } = 1000f;
        
        public Vector2 Velocity;
        private bool _isGrounded;
        private float _jumpBufferTimer;
        
        public Camera Camera { get; set; }
        public List<GameObject> Platforms { get; set; } // Simplified collision check

        private Player PlayerData => SimContext.Instance.Player;

        public override void Start()
        {
            // Initialize position from persistent player data
            Owner.Position = PlayerData.Position;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            // InputManager.Instance.Update(); // Handled by Scene/GameLoop

            // Horizontal Input
            Velocity.X = 0;
            if (InputManager.Instance.IsKeyDown(Keys.A)) Velocity.X = -MoveSpeed;
            if (InputManager.Instance.IsKeyDown(Keys.D)) Velocity.X = MoveSpeed;

            // Jump Buffering
            if (InputManager.Instance.IsJumpJustPressed())
            {
                _jumpBufferTimer = 0.1f; // Buffer for 0.1 seconds
            }

            if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= dt;
                if (_isGrounded)
                {
                    Velocity.Y = JumpStrength;
                    _isGrounded = false;
                    _jumpBufferTimer = 0; // Consume jump
                }
            }

            // Gravity
            Velocity.Y += Gravity * dt;

            // Apply and Resolve Collisions
            // Horizontal
            Owner.Position += new Vector2(Velocity.X * dt, 0);
            HandleCollisions(true);

            // Vertical
            Owner.Position += new Vector2(0, Velocity.Y * dt);
            _isGrounded = false;
            HandleCollisions(false);

            // Camera
            Camera.Follow(Owner.Position);

            // Sync position back to persistent player data
            PlayerData.Position = Owner.Position;
        }

        private void HandleCollisions(bool horizontal)
        {
            var myCollider = Owner.GetComponent<BoxCollider>();
            if (myCollider == null) return;

            Rectangle myBounds = myCollider.Bounds;

            foreach (var obj in Platforms)
            {
                var otherCollider = obj.GetComponent<BoxCollider>();
                if (otherCollider == null || obj == Owner) continue;

                if (myBounds.Intersects(otherCollider.Bounds))
                {
                    Rectangle platformBounds = otherCollider.Bounds;
                    if (horizontal)
                    {
                        if (Velocity.X > 0) Owner.Position = new Vector2(platformBounds.Left - myCollider.Width - myCollider.Offset.X, Owner.Position.Y);
                        else if (Velocity.X < 0) Owner.Position = new Vector2(platformBounds.Right - myCollider.Offset.X, Owner.Position.Y);
                        Velocity.X = 0; // Stop horizontal velocity on wall hit? 
                        // Actually, just resolve position.
                    }
                    else
                    {
                        if (Velocity.Y > 0) // Landing
                        {
                            Owner.Position = new Vector2(Owner.Position.X, platformBounds.Top - myCollider.Height - myCollider.Offset.Y);
                            _isGrounded = true;
                            Velocity.Y = 0;
                        }
                        else if (Velocity.Y < 0) // Ceiling
                        {
                            Owner.Position = new Vector2(Owner.Position.X, platformBounds.Bottom - myCollider.Offset.Y);
                            Velocity.Y = 0;
                        }
                    }
                    myBounds = myCollider.Bounds; // Update for next check if multiple collisions (simple iter)
                }
            }
        }
    }
}
