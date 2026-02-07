using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Components
{
    /// <summary>
    /// Represents an animation consisting of multiple frames from a sprite sheet.
    /// </summary>
    public class Animation
    {
        public string Name { get; }
        public int Row { get; }
        public int FrameCount { get; }
        public float FrameDuration { get; }
        public bool IsLooping { get; }

        /// <summary>
        /// Creates a new animation definition.
        /// </summary>
        /// <param name="name">Unique name for this animation (e.g., "idle_down", "walk_right")</param>
        /// <param name="row">Row index in the sprite sheet (0-indexed)</param>
        /// <param name="frameCount">Number of frames in this animation</param>
        /// <param name="frameDuration">Duration of each frame in seconds</param>
        /// <param name="isLooping">Whether the animation should loop</param>
        public Animation(string name, int row, int frameCount, float frameDuration = 0.1f, bool isLooping = true)
        {
            Name = name;
            Row = row;
            FrameCount = frameCount;
            FrameDuration = frameDuration;
            IsLooping = isLooping;
        }
    }

    /// <summary>
    /// Component that handles sprite sheet animation playback.
    /// Supports multiple named animations, frame timing, and sprite flipping.
    /// </summary>
    public class SpriteAnimator : Component
    {
        private Texture2D _spriteSheet;
        private Dictionary<string, Animation> _animations;
        private Animation _currentAnimation;
        private int _currentFrame;
        private float _frameTimer;
        private bool _isPlaying;
        private bool _animationComplete;

        /// <summary>
        /// Width of a single frame in pixels.
        /// </summary>
        public int FrameWidth { get; set; }

        /// <summary>
        /// Height of a single frame in pixels.
        /// </summary>
        public int FrameHeight { get; set; }

        /// <summary>
        /// Tint color applied to the sprite. Default is White (no tint).
        /// </summary>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Horizontal flip for facing left/right.
        /// </summary>
        public bool FlipHorizontal { get; set; }

        /// <summary>
        /// Vertical flip.
        /// </summary>
        public bool FlipVertical { get; set; }

        /// <summary>
        /// Scale factor for rendering. Default is 1.0.
        /// </summary>
        public float Scale { get; set; } = 1f;

        /// <summary>
        /// Origin point for rotation and positioning. Defaults to center of frame.
        /// </summary>
        public Vector2 Origin { get; set; }

        /// <summary>
        /// Playback speed multiplier. 1.0 = normal speed.
        /// </summary>
        public float PlaybackSpeed { get; set; } = 1f;

        /// <summary>
        /// Current frame index within the active animation.
        /// </summary>
        public int CurrentFrame => _currentFrame;

        /// <summary>
        /// Name of the currently playing animation.
        /// </summary>
        public string CurrentAnimationName => _currentAnimation?.Name;

        /// <summary>
        /// True if the current non-looping animation has finished.
        /// </summary>
        public bool IsAnimationComplete => _animationComplete;

        /// <summary>
        /// Event triggered when a non-looping animation completes.
        /// </summary>
        public event Action<string> OnAnimationComplete;

        /// <summary>
        /// Creates a new SpriteAnimator component.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet texture</param>
        /// <param name="frameWidth">Width of each frame in pixels</param>
        /// <param name="frameHeight">Height of each frame in pixels</param>
        public SpriteAnimator(Texture2D spriteSheet, int frameWidth, int frameHeight)
        {
            _spriteSheet = spriteSheet;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            _animations = new Dictionary<string, Animation>();
            Origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
            _isPlaying = true;
        }

        /// <summary>
        /// Adds an animation to this animator.
        /// </summary>
        public void AddAnimation(Animation animation)
        {
            _animations[animation.Name] = animation;

            // Auto-set first animation as current
            if (_currentAnimation == null)
            {
                _currentAnimation = animation;
            }
        }

        /// <summary>
        /// Adds multiple animations at once.
        /// </summary>
        public void AddAnimations(params Animation[] animations)
        {
            foreach (var anim in animations)
            {
                AddAnimation(anim);
            }
        }

        /// <summary>
        /// Switches to a different animation by name.
        /// </summary>
        /// <param name="animationName">Name of the animation to play</param>
        /// <param name="resetFrame">If true, restart from frame 0. If false, continue from current frame.</param>
        public void Play(string animationName, bool resetFrame = true)
        {
            if (!_animations.TryGetValue(animationName, out var animation))
            {
                return; // Animation not found
            }

            // Don't restart if already playing this animation (unless forced)
            if (_currentAnimation == animation && !resetFrame && _isPlaying)
            {
                return;
            }

            _currentAnimation = animation;
            _isPlaying = true;
            _animationComplete = false;

            if (resetFrame)
            {
                _currentFrame = 0;
                _frameTimer = 0f;
            }
        }

        /// <summary>
        /// Stops the animation at the current frame.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// Resumes a stopped animation.
        /// </summary>
        public void Resume()
        {
            _isPlaying = true;
        }

        /// <summary>
        /// Resets the current animation to its first frame.
        /// </summary>
        public void Reset()
        {
            _currentFrame = 0;
            _frameTimer = 0f;
            _animationComplete = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (_currentAnimation == null || !_isPlaying || _animationComplete)
            {
                return;
            }

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * PlaybackSpeed;

            if (_frameTimer >= _currentAnimation.FrameDuration)
            {
                _frameTimer -= _currentAnimation.FrameDuration;
                _currentFrame++;

                if (_currentFrame >= _currentAnimation.FrameCount)
                {
                    if (_currentAnimation.IsLooping)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = _currentAnimation.FrameCount - 1;
                        _animationComplete = true;
                        OnAnimationComplete?.Invoke(_currentAnimation.Name);
                    }
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_spriteSheet == null || _currentAnimation == null)
            {
                return;
            }

            // Calculate source rectangle from sprite sheet
            Rectangle sourceRect = new Rectangle(
                _currentFrame * FrameWidth,
                _currentAnimation.Row * FrameHeight,
                FrameWidth,
                FrameHeight
            );

            // Determine flip effects
            SpriteEffects effects = SpriteEffects.None;
            if (FlipHorizontal)
                effects |= SpriteEffects.FlipHorizontally;
            if (FlipVertical)
                effects |= SpriteEffects.FlipVertically;

            spriteBatch.Draw(
                _spriteSheet,
                Owner.Position,
                sourceRect,
                Color,
                0f,
                Origin,
                Scale,
                effects,
                0f
            );
        }

        /// <summary>
        /// Gets the current source rectangle (useful for collision detection based on sprite).
        /// </summary>
        public Rectangle GetCurrentSourceRect()
        {
            if (_currentAnimation == null)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                _currentFrame * FrameWidth,
                _currentAnimation.Row * FrameHeight,
                FrameWidth,
                FrameHeight
            );
        }
    }
}
