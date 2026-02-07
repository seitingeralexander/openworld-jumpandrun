using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using JumpAndRun.Core;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Collections.Generic;

namespace JumpAndRun.Scenes
{
    public class SideScrollScene : Scene
    {
        private GameObject _player;
        private Camera _camera;
        private GraphicsDevice _graphicsDevice;
        private ContentManager _content;
        private List<GameObject> _platforms;
        private Rectangle _exitPortal;
        
        public SideScrollScene(GraphicsDevice graphicsDevice, ContentManager content)
            : base(SimContext.Instance)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
            _platforms = new List<GameObject>();
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            // Textures
            Texture2D playerTex = new Texture2D(_graphicsDevice, 24, 48);
            Color[] playerData = new Color[24 * 48];
            for(int i=0; i<playerData.Length; i++) playerData[i] = Color.Blue;
            playerTex.SetData(playerData);

            // Create Platforms
            CreatePlatform(0, 400, 800, 20, Color.Gray); // Floor
            CreatePlatform(300, 300, 200, 20, Color.Brown); // Platform
            CreatePlatform(-20, 0, 20, 600, Color.Gray); // Left Wall

            // Exit portal (door back to town) - at the left edge
            _exitPortal = new Rectangle(0, 350, 30, 50);

            // Create Player Entity
            _player = new GameObject();
            _player.Position = new Vector2(100, 300);
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new BoxCollider(24, 48));
            _player.AddComponent(new SideScrollController() 
            { 
                Camera = _camera,
                Platforms = _platforms 
            });
        }

        private void CreatePlatform(int x, int y, int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(_graphicsDevice, width, height);
            Color[] data = new Color[width * height];
            for(int i=0; i<data.Length; i++) data[i] = Color.White;
            tex.SetData(data);

            var platform = new GameObject() { Position = new Vector2(x, y) };
            platform.AddComponent(new SpriteRenderer(tex) { Color = color });
            platform.AddComponent(new BoxCollider(width, height));
            
            _platforms.Add(platform);
        }

        public override void Update(GameTime gameTime)
        {
            _player.Update(gameTime);
            foreach(var p in _platforms) p.Update(gameTime);

            // Check exit portal collision
            CheckExitPortal();
        }

        private void CheckExitPortal()
        {
            var playerBounds = new Rectangle(
                (int)_player.Position.X - 12, 
                (int)_player.Position.Y - 24, 
                24, 48);

            if (playerBounds.Intersects(_exitPortal))
            {
                // Return to TownScene
                var townScene = SceneFactory.Create("TownScene");
                SceneManager.Instance.LoadScene(townScene);
            }
        }

        public override void UnloadContent()
        {
             // Dispose player texture
            var playerRenderer = _player?.GetComponent<SpriteRenderer>();
            playerRenderer?.Texture?.Dispose();

            // Dispose platform textures
            foreach (var platform in _platforms)
            {
                var renderer = platform.GetComponent<SpriteRenderer>();
                renderer?.Texture?.Dispose();
            }
            _platforms.Clear();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(transformMatrix: _camera.Transform);

            foreach (var platform in _platforms)
            {
                platform.Draw(spriteBatch);
            }

            // Draw exit portal (door)
            Texture2D pixel = new Texture2D(_graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, _exitPortal, Color.Purple);
            pixel.Dispose();

            _player.Draw(spriteBatch);

            // Draw exit label
            DebugFont.DrawString(spriteBatch, "EXIT", new Vector2(_exitPortal.X, _exitPortal.Y - 15), Color.White);

            spriteBatch.End();
        }
    }
}
