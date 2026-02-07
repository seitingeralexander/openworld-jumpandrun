using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JumpAndRun.Core;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Collections.Generic;

namespace JumpAndRun.Scenes
{
    public class TopDownScene : Scene
    {
        public override string SceneId => "TopDownScene";
        private GameObject _player; // Generic GameObject now
        private Camera _camera;
        private GraphicsDevice _graphicsDevice;
        private List<GameObject> _objects;
        
        // Assets
        private Texture2D _pixel;
        private Texture2D _houseTexture;

        public TopDownScene(GraphicsDevice graphicsDevice)
            : base(SimContext.Instance)
        {
            _graphicsDevice = graphicsDevice;
            _objects = new List<GameObject>();
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            // Textures
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _houseTexture = new Texture2D(_graphicsDevice, 32, 32);
            Color[] houseData = new Color[32 * 32];
            for(int i=0; i<houseData.Length; i++) houseData[i] = Color.Brown;
            _houseTexture.SetData(houseData);

            Texture2D playerTex = new Texture2D(_graphicsDevice, 16, 16);
            Color[] playerData = new Color[16 * 16];
            for(int i=0; i<playerData.Length; i++) playerData[i] = Color.Green;
            playerTex.SetData(playerData);

            // Create Player Entity
            _player = new GameObject();
            _player.Position = new Vector2(100, 100);
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new TopDownController() { Camera = _camera }); // Add Logic Component!
            _player.AddComponent(new BoxCollider(16, 16));


            // Create House Entity
            var house = new GameObject()
            {
                Position = new Vector2(300, 100)
            };
            house.AddComponent(new SpriteRenderer(_houseTexture));
            house.AddComponent(new BoxCollider(32, 32));
            _objects.Add(house);
        }

        public override void Update(GameTime gameTime)
        {
            // Update Player
            _player.Update(gameTime);

            // Update Objects
            foreach(var obj in _objects)
            {
                obj.Update(gameTime);
            }

            // Trigger Logic (Could be a System, but inline for now is fine for "Clean Code" request vs over-engineering)
            var playerCollider = _player.GetComponent<BoxCollider>();
            foreach(var obj in _objects)
            {
                 var objCollider = obj.GetComponent<BoxCollider>();
                 if (objCollider != null && playerCollider.Bounds.Intersects(objCollider.Bounds))
                 {
                     // Simple Trigger for now
                     _player.GetComponent<SpriteRenderer>().Color = Color.Blue;
                     SceneManager.Instance.LoadScene(SceneFactory.Create("SideScrollScene"));
                 }
                 else
                 {
                      _player.GetComponent<SpriteRenderer>().Color = Color.White;
                 }
            }
        }

        public override void UnloadContent()
        {
            _pixel?.Dispose();
            _houseTexture?.Dispose();
            
            // Dispose player texture
            var playerRenderer = _player?.GetComponent<SpriteRenderer>();
            playerRenderer?.Texture?.Dispose();
            
            // Dispose object textures if any (house is in _objects but we disposed _houseTexture already which is shared? No, specific instance)
            // Actually _houseTexture is the source. The SpriteRenderer holds a reference.
            // If we share textures, we should be careful. 
            // In LoadContent: _houseTexture is assigned to house. So disposing _houseTexture is enough for that one.
            // But verify if we have other textures.
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Ground
            spriteBatch.Draw(_pixel, new Rectangle(-1000, -1000, 2000, 2000), Color.DarkGreen);

            foreach(var obj in _objects)
            {
                obj.Draw(spriteBatch);
            }

            _player.Draw(spriteBatch);
            
            // Debug Visualization
            // Draw Line to Mouse
            Vector2 mousePos = InputManager.Instance.GetMousePosition();
            Vector2 mouseWorldPos = Vector2.Transform(mousePos, Matrix.Invert(_camera.Transform));
            DrawLine(spriteBatch, _player.Position, mouseWorldPos, Color.Red); // Red = To Mouse
            
            spriteBatch.End();
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness = 2)
        {
            Vector2 edge = end - start;
            float angle = (float)System.Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(edge.Length(), thickness), SpriteEffects.None, 0);
        }
    }
}
