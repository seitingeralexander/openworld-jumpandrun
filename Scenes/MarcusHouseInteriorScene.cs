using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using JumpAndRun.Core;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using JumpAndRun.Simulation;
using System.Collections.Generic;
using System.Linq;

namespace JumpAndRun.Scenes
{
    /// <summary>
    /// Interior scene for Marcus's house. Demonstrates building interior scenes.
    /// </summary>
    public class MarcusHouseInteriorScene : Scene
    {
        public override string SceneId => "MarcusHouseInterior";
        
        private GameObject _player;
        private Camera _camera;
        private GraphicsDevice _graphicsDevice;
        private ContentManager _content;
        private Texture2D _pixel;
        private Rectangle _exitDoor;

        public MarcusHouseInteriorScene(GraphicsDevice graphicsDevice, ContentManager content)
            : base(SimContext.Instance)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            DebugFont.Initialize(_graphicsDevice);

            // Create Player
            Texture2D playerTex = new Texture2D(_graphicsDevice, 16, 16);
            Color[] playerData = new Color[16 * 16];
            for (int i = 0; i < playerData.Length; i++) playerData[i] = Color.Green;
            playerTex.SetData(playerData);

            _player = new GameObject();
            _player.Position = new Vector2(200, 300); // Spawn near center
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new TopDownController() { Camera = _camera });
            _player.AddComponent(new BoxCollider(16, 16));

            // Exit door (back to town)
            _exitDoor = new Rectangle(180, 380, 40, 20);
        }

        public override void Update(GameTime gameTime)
        {
            _player.Update(gameTime);
            CheckExitDoor();
        }

        private void CheckExitDoor()
        {
            var playerBounds = new Rectangle(
                (int)_player.Position.X - 8,
                (int)_player.Position.Y - 8,
                16, 16);

            if (playerBounds.Intersects(_exitDoor))
            {
                // Return to TownScene at the house entrance (home_02)
                var homeLocation = Context.Town.GetLocation("home_02");
                if (homeLocation != null)
                {
                    // Offset player slightly from portal to prevent re-entry
                    Context.Player.TownPosition = homeLocation.Position + new Vector2(0, 30);
                }
                
                var townScene = SceneFactory.Create("TownScene");
                SceneManager.Instance.LoadScene(townScene);
            }
        }

        public override void UnloadContent()
        {
            _pixel?.Dispose();
            var playerRenderer = _player?.GetComponent<SpriteRenderer>();
            playerRenderer?.Texture?.Dispose();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(new Color(60, 40, 30)); // Dark interior color

            spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Draw floor
            spriteBatch.Draw(_pixel, new Rectangle(50, 50, 300, 350), new Color(80, 60, 40));

            // Draw walls
            spriteBatch.Draw(_pixel, new Rectangle(50, 50, 300, 10), Color.SaddleBrown);  // Top
            spriteBatch.Draw(_pixel, new Rectangle(50, 50, 10, 350), Color.SaddleBrown);  // Left
            spriteBatch.Draw(_pixel, new Rectangle(340, 50, 10, 350), Color.SaddleBrown); // Right
            spriteBatch.Draw(_pixel, new Rectangle(50, 390, 130, 10), Color.SaddleBrown); // Bottom left
            spriteBatch.Draw(_pixel, new Rectangle(220, 390, 130, 10), Color.SaddleBrown); // Bottom right

            // Draw bed (where NPC sleeps)
            spriteBatch.Draw(_pixel, new Rectangle(180, 100, 60, 100), Color.IndianRed);
            DebugFont.DrawString(spriteBatch, "Bed", new Vector2(195, 80), Color.White);

            // Draw exit door
            spriteBatch.Draw(_pixel, _exitDoor, Color.Brown);
            DebugFont.DrawString(spriteBatch, "EXIT", new Vector2(185, 405), Color.White);

            // Draw NPCs in this scene
            foreach (var npc in Context.NPCs.Where(n => n.CurrentSceneId == SceneId))
            {
                spriteBatch.Draw(_pixel, new Rectangle((int)npc.Position.X - 5, (int)npc.Position.Y - 5, 10, 10), Color.White);
                string info = $"{npc.Name} | {npc.State}";
                DebugFont.DrawString(spriteBatch, info, npc.Position + new Vector2(-30, 15), Color.White);
            }

            // Draw player
            _player.Draw(spriteBatch);

            spriteBatch.End();

            // UI layer
            spriteBatch.Begin();
            DebugFont.DrawString(spriteBatch, "Marcus's Home Interior", new Vector2(10, 10), Color.White);
            spriteBatch.End();
        }
    }
}
