using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.Simulation;

namespace JumpAndRun.Components
{
    /// <summary>
    /// Cardinal facing direction for 4-directional animations.
    /// </summary>
    public enum FacingDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    public class TopDownController : Component
    {
        public float Speed { get; set; } = 200f;
        public Camera Camera { get; set; } // Needed for mouse position conversion

        /// <summary>
        /// Current facing direction based on last movement.
        /// </summary>
        public FacingDirection Facing { get; private set; } = FacingDirection.Down;

        /// <summary>
        /// Whether the player is currently moving.
        /// </summary>
        public bool IsMoving { get; private set; }

        private Player PlayerData => SimContext.Instance.Player;
        private SpriteAnimator _animator;

        public override void Start()
        {
            // Initialize position from persistent player data (TownPosition for top-down scenes)
            Owner.Position = PlayerData.TownPosition;

            // Cache animator reference
            _animator = Owner.GetComponent<SpriteAnimator>();
        }

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

            IsMoving = moveDir != Vector2.Zero;

            if (IsMoving)
            {
                moveDir.Normalize();
                Owner.Position += moveDir * Speed * dt;

                // Determine facing direction based on movement
                UpdateFacingDirection(moveDir);
            }
            
            Camera.Follow(Owner.Position);

            // Update animation based on state
            UpdateAnimation();

            // Sync position back to persistent player data (TownPosition for top-down scenes)
            PlayerData.TownPosition = Owner.Position;
            PlayerData.Position = Owner.Position; // Also update general position for compatibility
        }

        private void UpdateFacingDirection(Vector2 moveDir)
        {
            // Determine primary direction based on which axis has larger magnitude
            if (Math.Abs(moveDir.X) > Math.Abs(moveDir.Y))
            {
                Facing = moveDir.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            }
            else
            {
                Facing = moveDir.Y > 0 ? FacingDirection.Down : FacingDirection.Up;
            }
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            string animPrefix = IsMoving ? "walk" : "idle";
            string direction = Facing.ToString().ToLowerInvariant();
            string animName = $"{animPrefix}_{direction}";

            if (_animator.CurrentAnimationName != animName)
            {
                _animator.Play(animName, resetFrame: true);
            }
        }
    }
}

