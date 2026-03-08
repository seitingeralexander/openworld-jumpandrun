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
using JumpAndRun.WorldGen;

namespace JumpAndRun.Scenes
{
    public class TownScene : Scene
    {
        public override string SceneId { get; }
        private ContentManager _content;
        private GraphicsDevice _graphicsDevice;
        private Texture2D _pixel;
        private GameObject _player;
        private Camera _camera;
        
        // Rendering resources for Voronoi Town
        private BasicEffect _basicEffect;
        private List<VertexPositionColor> _vertices = new();
        private List<int> _indices = new();
        private List<VertexPositionColor> _borderVertices = new();
        private List<VertexPositionColor> _wallVertices = new();
        private List<VertexPositionColor> _roadVertices = new();

        public TownScene(GraphicsDevice graphicsDevice, ContentManager content, SimContext context, string sceneId = "TownScene")
            : base(context)
        {
            SceneId = sceneId;
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

            // Lazy Load Town Generation if locations don't exist for this specific SceneId
            if (SceneId != "OpenWorldScene" && !Context.Town.Locations.Any(l => l.SceneId == SceneId))
            {
                // We need the global seed. For now we can extract it or pass it. 
                // Let's use 42 as a fallback, or fetch it if we store it.
                // Assuming SimContext or Game state handles seed, but we can just use 42 for now.
                JumpAndRun.WorldGen.TownGenerator.GenerateTown(SceneId, 42, Context);
            }

            // Initialize BasicEffect for polygon rendering
            _basicEffect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(
                    0, _graphicsDevice.Viewport.Width,
                    _graphicsDevice.Viewport.Height, 0,
                    0, 1) // Default UI/2D ortho, but changed dynamically in Draw
            };

            BuildTownMesh();

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

        private void BuildTownMesh()
        {
            if (!Context.Town.TownMaps.ContainsKey(SceneId)) return;
            var mapGen = Context.Town.TownMaps[SceneId];

            _vertices.Clear();
            _indices.Clear();
            _borderVertices.Clear();
            _wallVertices.Clear();
            _roadVertices.Clear();

            // 1. Draw Polygons
            foreach (var center in mapGen.Centers)
            {
                Color color = GetColorForDistrict(center.District);

                var sortedCorners = center.Corners.OrderBy(c => System.Math.Atan2(c.Point.Y - center.Point.Y, c.Point.X - center.Point.X)).ToList();

                if (sortedCorners.Count < 3) continue;

                int startIndex = _vertices.Count;
                _vertices.Add(new VertexPositionColor(new Vector3(center.Point, 0), color));

                for (int i = 0; i < sortedCorners.Count; i++)
                {
                    var current = sortedCorners[i];
                    var next = sortedCorners[(i + 1) % sortedCorners.Count];

                    _vertices.Add(new VertexPositionColor(new Vector3(current.Point, 0), color));
                    
                    _indices.Add(startIndex);
                    _indices.Add(_vertices.Count - 1);
                    _indices.Add((i + 1 == sortedCorners.Count) ? startIndex + 1 : _vertices.Count);
                    
                    // Add border lines (light thin lines for all polygons)
                    _borderVertices.Add(new VertexPositionColor(new Vector3(current.Point, 0), Color.DarkGray * 0.3f));
                    _borderVertices.Add(new VertexPositionColor(new Vector3(next.Point, 0), Color.DarkGray * 0.3f));
                }
            }

            // 2. Draw Walls and Roads
            foreach (var edge in mapGen.Edges)
            {
                if (edge.IsWall)
                {
                    // Draw a thick/bright wall line
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V0.Point, 0), Color.DarkSlateGray));
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V1.Point, 0), Color.DarkSlateGray));
                    
                    // Add a slight offset to make it "thicker"
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V0.Point.X + 2, edge.V0.Point.Y + 2, 0), Color.Gray));
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V1.Point.X + 2, edge.V1.Point.Y + 2, 0), Color.Gray));
                    
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V0.Point.X - 2, edge.V0.Point.Y - 2, 0), Color.Gray));
                    _wallVertices.Add(new VertexPositionColor(new Vector3(edge.V1.Point.X - 2, edge.V1.Point.Y - 2, 0), Color.Gray));
                }
                
                if (edge.Road > 0)
                {
                    Color roadColor = new Color(139, 115, 85); // Light dirt/cobble color 
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point, 0), roadColor));
                    
                    // Thicken road
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point.X + 3, edge.D0.Point.Y + 3, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point.X + 3, edge.D1.Point.Y + 3, 0), roadColor));
                    
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point.X - 3, edge.D0.Point.Y - 3, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point.X - 3, edge.D1.Point.Y - 3, 0), roadColor));
                }
            }
        }

        private Color GetColorForDistrict(DistrictType district)
        {
            return district switch
            {
                DistrictType.Market => new Color(200, 150, 100), // Sandy / Cobble
                DistrictType.Noble => new Color(150, 200, 150), // Nice green lawns
                DistrictType.Residential => new Color(120, 100, 80), // Dirt/wood
                DistrictType.Slum => new Color(80, 70, 60), // Dark mud
                DistrictType.Farm => new Color(180, 200, 80), // Bright green/yellow crops
                DistrictType.Military => new Color(100, 100, 110), // Stone/Iron
                DistrictType.Wilderness => new Color(50, 120, 50), // Forest green outside walls
                _ => Color.Black
            };
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
                    // When leaving a town, we need to ensure the player's world position is outside the OpenWorld's 300px entry radius.
                    // Instead of a tiny offset, push them 400 pixels away from the town center in the Open World map.
                    var townLoc = Context.Town.Locations.FirstOrDefault(l => l.Id == SceneId);
                    if (townLoc != null)
                    {
                        Vector2 awayFromTown = _player.Position - new Vector2(1500, 1500); // Town map center
                        if (awayFromTown != Vector2.Zero) awayFromTown.Normalize();
                        else awayFromTown = new Vector2(0, 1);
                        
                        Context.Player.Position = townLoc.Position + awayFromTown * 400f; // 400px > 300px trigger radius
                    }
                    
                    var scene = SceneFactory.Create(loc.TargetSceneId);
                    SceneManager.Instance.LoadScene(scene);
                    return; // Exit immediately after scene change
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.Black);

            // Update effect matrices for 2D Camera
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0, _graphicsDevice.Viewport.Width,
                _graphicsDevice.Viewport.Height, 0,
                0, 1);
            _basicEffect.View = _camera.Transform;

            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                // Draw filled polygons
                if (_indices.Count > 0)
                {
                    _graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertices.ToArray(), 0, _vertices.Count,
                        _indices.ToArray(), 0, _indices.Count / 3);
                }

                // Draw district borders
                if (_borderVertices.Count > 0)
                {
                    _graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        _borderVertices.ToArray(), 0, _borderVertices.Count / 2);
                }

                // Draw Town Walls ( thicker visually by drawing multiple lines or just bright color)
                if (_wallVertices.Count > 0)
                {
                    // For thicker lines, we'd need a geometry shader or manual quads, but a line list is okay for now
                    _graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        _wallVertices.ToArray(), 0, _wallVertices.Count / 2);
                }

                // Draw Roads
                if (_roadVertices.Count > 0)
                {
                    _graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        _roadVertices.ToArray(), 0, _roadVertices.Count / 2);
                }
            }

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
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - 35, (int)loc.Position.Y - 35, 70, 70), color);
                
                if (true) // DebugFont is always available
                {
                    Vector2 textSize = DebugFont.MeasureString(loc.Name);
                    DebugFont.DrawString(spriteBatch, loc.Name, loc.Position - new Vector2(textSize.X / 2, 55), Color.White);
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
