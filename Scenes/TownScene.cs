using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JumpAndRun.Core;
using JumpAndRun.World;
using JumpAndRun.Simulation;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Collections.Generic;

namespace JumpAndRun.Scenes
{
    public class TownScene : Scene
    {
        private SimContext _context;
        private NPCSystem _npcSystem;
        // private SpriteFont _font; (Unused)
        private Texture2D _pixel;
        private GraphicsDevice _graphicsDevice;

        private GameObject _player;
        private Camera _camera;

        public TownScene(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _context = new SimContext();
            _npcSystem = new NPCSystem(_context);
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

                        // Load Font (assuming one exists, or we use a default simple drawing if not available)
            // For now, let's try to load a font, but if it fails, we need a fallback or ensure it exists.
            // The user project structure shows Content folder.
            // Let's assume "File" font exists or we just draw rectangles for now if font fails.
            // Actually, without a font, debug info is hard.
            // I'll try to load "File" or "Arial" if standard.
            // Let's assume a default font "File" or similar from previous scenes?
            // Checking previous file list... no font file explicitly seen in root, but Content folder exists.
            // I'll just use a basic primitive drawing for now and maybe try to load a font.
            // If I can't load a font, I'll draw bars/colors to represent state.


            // Create Player Entity
            Texture2D playerTex = new Texture2D(_graphicsDevice, 16, 16);
            Color[] playerData = new Color[16 * 16];
            for(int i=0; i<playerData.Length; i++) playerData[i] = Color.Green;
            playerTex.SetData(playerData);

            _player = new GameObject();
            _player.Position = new Vector2(200, 200);
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new TopDownController() { Camera = _camera });
            _player.AddComponent(new BoxCollider(16, 16));
            
            // Setup Town
            SetupWorld();
        }

        private void SetupWorld()
        {
            // 1. Create Locations
            var home = new Location("home_01", "Baker's Home", new Vector2(100, 100), LocationType.Home);
            home.SetNeedRate(NeedType.Energy, 10f); // Sleeps restores 10/sec

            var bakery = new Location("bakery_01", "Bakery", new Vector2(300, 100), LocationType.Work);
            
            var market = new Location("market_01", "Market", new Vector2(300, 300), LocationType.Service);
            market.SetNeedRate(NeedType.Hunger, 20f); // Eating restores 20/sec
            
            var tavern = new Location("tavern_01", "Tavern", new Vector2(100, 300), LocationType.Leisure);
            tavern.SetNeedRate(NeedType.Social, 15f);

            _context.Town.AddLocation(home);
            _context.Town.AddLocation(bakery);
            _context.Town.AddLocation(market);
            _context.Town.AddLocation(tavern);

            // 2. Create NPC
            var bg = new Background("Baker", "Hardworking", "home_01");
            var npc = new NPC("Elena", bg);
            npc.Position = home.Position; // Start at home

            // 3. Define Schedule
            npc.Schedule.AddBlock(new ScheduleBlock(6, 12, ScheduleAction.Work, "bakery_01"));
            npc.Schedule.AddBlock(new ScheduleBlock(12, 13, ScheduleAction.Eat, "market_01"));
            npc.Schedule.AddBlock(new ScheduleBlock(13, 18, ScheduleAction.Work, "bakery_01"));
            npc.Schedule.AddBlock(new ScheduleBlock(18, 22, ScheduleAction.Socialize, "tavern_01"));
            npc.Schedule.AddBlock(new ScheduleBlock(22, 6, ScheduleAction.Sleep, "home_01"));

            _context.NPCs.Add(npc);
        }

        public override void Update(GameTime gameTime)
        {
            // Update Time
            // Speed up time for testing: 1 real sec = 60 game mins (1 hour) -> 60x faster than default 0.1s/min
            // Default: 0.1s = 1 min => 6s = 1 hour.
            _context.Time.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            // Update Simulation
            _npcSystem.Update(gameTime);

            // Update Player
            _player.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.DarkOliveGreen);

            spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Draw Locations
            foreach (var loc in _context.Town.Locations)
            {
                Color color = Color.Gray;
                switch (loc.Type)
                {
                    case LocationType.Home: color = Color.Blue; break;
                    case LocationType.Work: color = Color.Orange; break;
                    case LocationType.Service: color = Color.Green; break;
                    case LocationType.Leisure: color = Color.Purple; break;
                }
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - 20, (int)loc.Position.Y - 20, 40, 40), color);
            }

            // Draw NPC
            foreach (var npc in _context.NPCs)
            {
                spriteBatch.Draw(_pixel, new Rectangle((int)npc.Position.X - 5, (int)npc.Position.Y - 5, 10, 10), Color.White);
                
                // Draw Needs Bar (Health-like)
                DrawBar(spriteBatch, new Vector2(npc.Position.X - 10, npc.Position.Y - 15), npc.Needs.GetValue(NeedType.Hunger) / 100f, Color.Red);
                DrawBar(spriteBatch, new Vector2(npc.Position.X - 10, npc.Position.Y - 20), npc.Needs.GetValue(NeedType.Energy) / 100f, Color.Yellow);
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
            int timeWidth = (int)((_context.Time.Hour * 60 + _context.Time.Minute) / (24f * 60f) * 800);
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, timeWidth, 10), Color.White);
            spriteBatch.End();
        }

        private void DrawBar(SpriteBatch spriteBatch, Vector2 position, float curr, Color color)
        {
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, 20, 4), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(20 * curr), 4), color);
        }
    }
}
