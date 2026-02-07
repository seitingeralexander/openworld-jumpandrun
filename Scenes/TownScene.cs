using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.World;
using JumpAndRun.Simulation;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Collections.Generic;
using System.Linq;

namespace JumpAndRun.Scenes
{
    public class TownScene : Scene
    {
        public override string SceneId => "TownScene";
        private ContentManager _content;
        private GraphicsDevice _graphicsDevice;
        private Texture2D _pixel;
        private GameObject _player;
        private Camera _camera;

        public TownScene(GraphicsDevice graphicsDevice, ContentManager content, SimContext context)
            : base(context)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Initialize DebugFont
            DebugFont.Initialize(_graphicsDevice);

            try
            {
                // Load SpriteFont (Standard)
                //_font = _content.Load<SpriteFont>("File");
            }
            catch
            {
                // Fallback to debug font if load fails
                DebugFont.Initialize(_graphicsDevice);
            }


            // Create Player Entity (visual representation for this scene)
            Texture2D playerTex = new Texture2D(_graphicsDevice, 16, 16);
            Color[] playerData = new Color[16 * 16];
            for(int i=0; i<playerData.Length; i++) playerData[i] = Color.Green;
            playerTex.SetData(playerData);

            _player = new GameObject();
            // Position is set by TopDownController.Start() from Context.Player.Position
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new TopDownController() { Camera = _camera });
            _player.AddComponent(new BoxCollider(16, 16));
        }



        public override void Update(GameTime gameTime)
        {
            // Simulation (Time, NPCs) is now updated by Game1 via SimContext.Update()
            // Scene only handles player input and camera

            // Update Player
            _player.Update(gameTime);

            // Check portal collisions
            CheckPortalCollisions();

            // === Camera Zoom Controls ===
            // Keyboard: + / = to zoom in, - to zoom out, 0 to reset
            if (InputManager.Instance.IsKeyPressed(Keys.OemPlus) || InputManager.Instance.IsKeyPressed(Keys.Add))
            {
                _camera.ZoomIn();
            }
            if (InputManager.Instance.IsKeyPressed(Keys.OemMinus) || InputManager.Instance.IsKeyPressed(Keys.Subtract))
            {
                _camera.ZoomOut();
            }
            if (InputManager.Instance.IsKeyPressed(Keys.D0) || InputManager.Instance.IsKeyPressed(Keys.NumPad0))
            {
                _camera.ResetZoom();
            }

            // Mouse scroll wheel: scroll up (positive) = zoom in, scroll down (negative) = zoom out
            int scrollDelta = InputManager.Instance.GetScrollDelta();
            if (scrollDelta > 0)
            {
                _camera.ZoomIn();
            }
            else if (scrollDelta < 0)
            {
                _camera.ZoomOut();
            }
        }

        private void CheckPortalCollisions()
        {
            foreach (var loc in Context.Town.Locations)
            {
                if (!loc.IsPortal) continue;
                
                float distance = Vector2.Distance(_player.Position, loc.Position);
                if (distance < 30f) // Portal trigger radius
                {
                    // Offset player position slightly away from portal to prevent re-entry on return
                    Vector2 awayFromPortal = _player.Position - loc.Position;
                    if (awayFromPortal != Vector2.Zero) awayFromPortal.Normalize();
                    Context.Player.TownPosition = loc.Position + awayFromPortal * 50f;
                    
                    var scene = SceneFactory.Create(loc.TargetSceneId);
                    SceneManager.Instance.LoadScene(scene);
                    return; // Exit immediately after scene change
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.DarkOliveGreen);

            spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Draw Locations - only those in this scene
            foreach (var loc in Context.Town.Locations.Where(l => l.SceneId == SceneId))
            {
                Color color = Color.Gray;
                switch (loc.Type)
                {
                    case LocationType.Home: color = Color.Blue; break;
                    case LocationType.Work: color = Color.Orange; break;
                    case LocationType.Service: color = Color.Green; break;
                    case LocationType.Leisure: color = Color.Purple; break;
                    case LocationType.Portal: color = Color.Magenta; break;
                }
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - 20, (int)loc.Position.Y - 20, 40, 40), color);
                
                if (true) // DebugFont is always available
                {
                    Vector2 textSize = DebugFont.MeasureString(loc.Name);
                    DebugFont.DrawString(spriteBatch, loc.Name, loc.Position - new Vector2(textSize.X / 2, 40), Color.White);
                }
            }

            // Draw NPCs - only those in this scene
            foreach (var npc in Context.NPCs.Where(n => n.CurrentSceneId == SceneId))
            {
                spriteBatch.Draw(_pixel, new Rectangle((int)npc.Position.X - 5, (int)npc.Position.Y - 5, 10, 10), Color.White);
                
                // Draw Needs Bar (Health-like) - above the NPC
                DrawBar(spriteBatch, new Vector2(npc.Position.X - 10, npc.Position.Y - 15), npc.Needs.GetValue(NeedType.Hunger) / 100f, Color.Red);
                DrawBar(spriteBatch, new Vector2(npc.Position.X - 10, npc.Position.Y - 20), npc.Needs.GetValue(NeedType.Energy) / 100f, Color.Yellow);

                // Get current schedule block
                var currentBlock = npc.Schedule.GetBlockForHour(Context.Time.Hour);
                string scheduleInfo = currentBlock != null 
                    ? $"{currentBlock.Action} @ {currentBlock.TargetLocationId ?? "?"}" 
                    : "No schedule";

                // Draw info BELOW the NPC
                string info = $"{npc.Name} | {npc.State}\n{scheduleInfo}";
                Vector2 infoSize = DebugFont.MeasureString(info);
                DebugFont.DrawString(spriteBatch, info, npc.Position + new Vector2(-infoSize.X / 2, 15), Color.White);
            }

            // Draw Player
            _player.Draw(spriteBatch);

            // Draw Time Info
            // We need to transform this back to screen space or draw in a separate batch if we want it UI-fixed.
            // For now, let's draw it in world space but maybe above everything, or switch batch.
            // Using a second batch for UI is better.
            spriteBatch.End();

            spriteBatch.Begin(); // UI Batch (No camera transform)

            // Draw Time Info (Simple Representation if no font)
            // Draw a clock hand or progress bar for day?
            int timeWidth = (int)((Context.Time.Hour * 60 + Context.Time.Minute) / (24f * 60f) * 800);
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, timeWidth, 10), Color.White);

            // Draw Zoom Level Indicator
            string zoomText = $"Zoom: {_camera.Zoom:F1}x (+/- or Scroll, 0 to reset)";
            DebugFont.DrawString(spriteBatch, zoomText, new Vector2(10, 20), Color.White);

            spriteBatch.End();
        }

        private void DrawBar(SpriteBatch spriteBatch, Vector2 position, float curr, Color color)
        {
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, 20, 4), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(20 * curr), 4), color);
        }
    }
}
