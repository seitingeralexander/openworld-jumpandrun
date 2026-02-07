using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JumpAndRun.Core
{
    public class InputManager
    {
        private static InputManager _instance;
        public static InputManager Instance => _instance ??= new InputManager();

        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;

        private InputManager() { }

        public void Update()
        {
            _previousKeyboardState = _currentKeyboardState;
            if (_isTesting)
                _currentKeyboardState = _simulatedKeyboardState;
            else
                _currentKeyboardState = Keyboard.GetState();

            _previousMouseState = _currentMouseState;
            if (_isTesting)
                _currentMouseState = _simulatedMouseState;
            else
                _currentMouseState = Mouse.GetState();
        }

        // Test Helpers
        private bool _isTesting = false;
        private KeyboardState _simulatedKeyboardState;
        private MouseState _simulatedMouseState;

        public void EnableTestMode()
        {
            _isTesting = true;
            _simulatedKeyboardState = new KeyboardState();
            _simulatedMouseState = new MouseState();
        }

        public void DisableTestMode()
        {
             _isTesting = false;
        }

        public void SimulateKeyDown(params Keys[] keys)
        {
            _simulatedKeyboardState = new KeyboardState(keys);
        }

        public void SimulateKeyUp(params Keys[] keys)
        {
             // Simple replacement for now, usually we'd merge
             // For test simplicity, SetKeys overrides everything
             _simulatedKeyboardState = new KeyboardState();
        }

        public void SimulateMousePosition(int x, int y)
        {
            _simulatedMouseState = new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        }

        public Vector2 GetMovementInput()
        {
            Vector2 movement = Vector2.Zero;

            if (_currentKeyboardState.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (_currentKeyboardState.IsKeyDown(Keys.S)) movement.Y += 1;
            if (_currentKeyboardState.IsKeyDown(Keys.A)) movement.X -= 1;
            if (_currentKeyboardState.IsKeyDown(Keys.D)) movement.X += 1;

            if (movement != Vector2.Zero)
                movement.Normalize();

            return movement;
        }

        public Vector2 GetMousePosition()
        {
            return new Vector2(_currentMouseState.X, _currentMouseState.Y);
        }

        public bool IsJumpJustPressed()
        {
            return IsKeyPressed(Keys.Space);
        }

        public bool IsKeyPressed(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
        }
        
        public bool IsKeyDown(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// Gets the scroll wheel delta since last frame.
        /// Positive = scroll up (zoom in), Negative = scroll down (zoom out)
        /// </summary>
        public int GetScrollDelta()
        {
            return _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        }
    }
}
