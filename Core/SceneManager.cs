using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JumpAndRun.Core
{
    public class SceneManager
    {
        private static SceneManager _instance;
        public static SceneManager Instance => _instance ??= new SceneManager();

        private Scene _currentScene;

        private SceneManager() { }

        public void LoadScene(Scene scene)
        {
            _currentScene?.UnloadContent();
            _currentScene = scene;
            _currentScene.LoadContent();
        }

        public void Update(GameTime gameTime)
        {
            _currentScene?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _currentScene?.Draw(spriteBatch);
        }
    }
}
