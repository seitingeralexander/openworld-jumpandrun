using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.WorldGen;
using JumpAndRun.Simulation;
using JumpAndRun.World;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Linq;

namespace JumpAndRun.Scenes
{
    public class OpenWorldScene : Scene
    {
        public override string SceneId => "OpenWorldScene";

        private GraphicsDevice _graphicsDevice;
        private ContentManager _content;
        private Camera _camera;
        private Vector2 _cameraPosition;
        private MapGenerator _mapGen;

        private BasicEffect _basicEffect;
        private List<VertexPositionColor> _vertices = new();
        private List<int> _indices = new();
        
        private List<VertexPositionColor> _borderVertices = new();
        private List<VertexPositionColor> _roadVertices = new();
        private GameObject _player;
        private int _seed = 42;
        private IslandShapeType _currentShape = IslandShapeType.Radial;
        private float _portalCooldown = 0f; // Grace period on scene load to prevent instant re-entry
        
        private Texture2D _pixel;

        public OpenWorldScene(GraphicsDevice graphicsDevice, ContentManager content, SimContext context)
            : base(context)
        {
            _graphicsDevice = graphicsDevice;
            _content = content;
            _camera = new Camera(_graphicsDevice.Viewport);
        }

        public override void LoadContent()
        {
            DebugFont.Initialize(_graphicsDevice);

            _basicEffect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height, 0, 0, 1),
                World = Matrix.Identity
            };

            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _basicEffect.VertexColorEnabled = true;

            // Player Texture
            Texture2D playerTex = new Texture2D(_graphicsDevice, 16, 16);
            Color[] playerData = new Color[16 * 16];
            for(int i=0; i<playerData.Length; i++) playerData[i] = Color.Blue;
            playerTex.SetData(playerData);

            _player = new GameObject();
            _player.AddComponent(new SpriteRenderer(playerTex));
            _player.AddComponent(new TopDownController() { Camera = _camera, Speed = 200f, UseTownPosition = false }); // Same speed as town
            _player.AddComponent(new BoxCollider(16, 16));

            GenerateWorld();
            _portalCooldown = 1.0f; // Give 1 second grace period before checking portals
        }

        private void GenerateWorld()
        {
            _mapGen = new MapGenerator(seed: _seed, variant: _seed, shape: _currentShape, numPoints: 5000, width: 10000, height: 10000);
            _mapGen.Generate();

            var worldBuilder = new WorldBuilder(_seed);
            worldBuilder.PopulateSimContext(Context, _mapGen);

            BuildRenderMesh();

            // Place camera in the center
            _cameraPosition = new Vector2(1000, 1000);
        }

        private void BuildRenderMesh()
        {
            _vertices.Clear();
            _indices.Clear();
            _borderVertices.Clear();
            _roadVertices.Clear();

            // Generate Triangles for each Voronoi polygon (Center)
            foreach (var center in _mapGen.Centers)
            {
                // To draw a convex polygon, we can fan out from the center point
                
                Color color = GetColorForBiome(center.Biome, center.Elevation);

                // Add Center vertex
                int startIndex = _vertices.Count;
                _vertices.Add(new VertexPositionColor(new Vector3(center.Point * 10f, 0), color));

                // Sort corners clockwise or counter-clockwise
                var sortedCorners = new List<Corner>(center.Corners);
                sortedCorners.Sort((a, b) => 
                {
                    double angleA = Math.Atan2(a.Point.Y - center.Point.Y, a.Point.X - center.Point.X);
                    double angleB = Math.Atan2(b.Point.Y - center.Point.Y, b.Point.X - center.Point.X);
                    return angleA.CompareTo(angleB);
                });

                for (int i = 0; i < sortedCorners.Count; i++)
                {
                    var current = sortedCorners[i];
                    var next = sortedCorners[(i + 1) % sortedCorners.Count];

                    _vertices.Add(new VertexPositionColor(new Vector3(current.Point * 10f, 0), color));
                    
                    _indices.Add(startIndex);
                    _indices.Add(_vertices.Count - 1);
                    _indices.Add((i + 1 == sortedCorners.Count) ? startIndex + 1 : _vertices.Count);
                    
                    // Add border lines directly connecting the scaled corners
                    Color borderColor = Color.Lerp(color, Color.Black, 0.2f);
                    if (current.Water || current.Ocean) borderColor = color;
                    else if (current.Coast) borderColor = Color.SandyBrown;
                    
                    _borderVertices.Add(new VertexPositionColor(new Vector3(current.Point * 10f, 0), borderColor));
                    _borderVertices.Add(new VertexPositionColor(new Vector3(next.Point * 10f, 0), borderColor));
                }
            }

            // Generate Roads
            Color roadColor = new Color(74, 55, 40); // Dark brown dirt road color
            foreach (var edge in _mapGen.Edges)
            {
                if (edge.Road > 0)
                {
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point * 10f, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point * 10f, 0), roadColor));

                    // Thicken road visually due to larger scale
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point.X * 10f + 10, edge.D0.Point.Y * 10f + 10, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point.X * 10f + 10, edge.D1.Point.Y * 10f + 10, 0), roadColor));

                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point.X * 10f - 10, edge.D0.Point.Y * 10f - 10, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point.X * 10f - 10, edge.D1.Point.Y * 10f - 10, 0), roadColor));
                }
            }

            // Move player to the first generated Home location if this is their first time
            if (!Context.Player.HasSpawnedInOpenWorld)
            {
                var homeLoc = Context.Town.Locations.FirstOrDefault(l => l.Type == LocationType.Home && l.SceneId == "OpenWorldScene");
                if (homeLoc != null)
                {
                    _player.Position = homeLoc.Position;
                    Context.Player.Position = homeLoc.Position;
                    Context.Player.HasSpawnedInOpenWorld = true;
                }
            }
        }

        private Color GetColorForBiome(string biome, float elevation)
        {
            // Colors from AmitP's polygon map generation Chapter 7: Biomes
            return biome switch
            {
                // Water
                "Ocean" => new Color(0x44, 0x44, 0x7a), // #44447a
                "Lake" => new Color(0x33, 0x66, 0x99), // #336699
                "Beach" => new Color(0xa0, 0x90, 0x77), // #a09077

                // High Elevation (> 0.8)
                "Snow" => new Color(0xff, 0xff, 0xff), // #ffffff
                "Tundra" => new Color(0xdd, 0xdd, 0xbb), // #ddddbb
                "Bare" => new Color(0xbb, 0xbb, 0xbb), // #bbbbbb
                "Scorched" => new Color(0x99, 0x99, 0x99), // #999999

                // Mid Elevation (> 0.6)
                "Taiga" => new Color(0xcc, 0xd4, 0xbb), // #ccd4bb
                "Shrubland" => new Color(0xc4, 0xcc, 0xbb), // #c4ccbb
                "TemperateDesert" => new Color(0xe4, 0xe8, 0xca), // #e4e8ca

                // Low-Mid Elevation (> 0.3)
                "TemperateRainForest" => new Color(0xa4, 0xc4, 0xa8), // #a4c4a8
                "TemperateDeciduousForest" => new Color(0xb4, 0xc9, 0xa9), // #b4c9a9
                "Grassland" => new Color(0xc4, 0xd4, 0xaa), // #c4d4aa
                // "TemperateDesert" covered above

                // Low Elevation (< 0.3)
                "TropicalRainForest" => new Color(0x9c, 0xbb, 0xa8), // #9cbba8
                "TropicalSeasonalForest" => new Color(0xa9, 0xcc, 0xa4), // #a9cca4
                // "Grassland" covered above
                "SubtropicalDesert" => new Color(0xe9, 0xdd, 0xc7), // #e9ddc7

                _ => Color.Magenta // Fallback
            };
        }

        public override void Update(GameTime gameTime)
        {
            // Camera zoom controls
            var kbd = InputManager.Instance;

            if (kbd.IsKeyPressed(Keys.OemPlus) || kbd.IsKeyPressed(Keys.Add)) _camera.ZoomIn();
            if (kbd.IsKeyPressed(Keys.OemMinus) || kbd.IsKeyPressed(Keys.Subtract)) _camera.ZoomOut();
            if (kbd.IsKeyPressed(Keys.D0) || kbd.IsKeyPressed(Keys.NumPad0)) _camera.ResetZoom();

            if (kbd.IsKeyPressed(Keys.R))
            {
                _seed = new Random().Next();
                GenerateWorld();
            }

            if (kbd.IsKeyPressed(Keys.T))
            {
                int nextShape = ((int)_currentShape + 1) % Enum.GetValues(typeof(IslandShapeType)).Length;
                _currentShape = (IslandShapeType)nextShape;
                GenerateWorld();
            }

            int scrollDelta = kbd.GetScrollDelta();
             if (scrollDelta > 0)
                _camera.ZoomIn();
            else if (scrollDelta < 0)
                _camera.ZoomOut();
                
            Vector2 previousPos = _player.Position;

            // Update Player (which handles WASD via TopDownController)
            _player.Update(gameTime);
            
            // Check impassable terrain
            if (_mapGen.Centers.Count > 0)
            {
                // Find closest center by scaling its point up to visual size
                var nearestCenter = _mapGen.Centers.OrderBy(c => Vector2.DistanceSquared(c.Point * 10f, _player.Position)).First();
                if (nearestCenter.Ocean || nearestCenter.Water || nearestCenter.Elevation > 0.8f) // 0.8 is Mountain/Snow
                {
                    _player.Position = previousPos; // Revert movement
                    Context.Player.TownPosition = previousPos;
                }
            }

            // Camera follows player
            _cameraPosition = _player.Position;
            // Removed _camera.Follow() here because TopDownController.Update() already calls Camera.Follow(Owner.Position)

            // Tick down the portal cooldown (prevents immediate re-entry after returning from a town)
            if (_portalCooldown > 0f)
            {
                _portalCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                return; // Skip all collision checks while cooling down
            }

            // Check location collisions (Transitions to TownScene)
            foreach (var loc in Context.Town.Locations)
            {
                if (loc.SceneId != "OpenWorldScene") continue;

                // Use a larger collision radius (300 pixels) because towns are placed in the center of 3000x3000 polygons now
                if (Vector2.DistanceSquared(_player.Position, loc.Position) < 90000f) // 300^2
                {
                    // Move the player back slightly to prevent a continuous transition loop upon return
                    Vector2 awayFromLoc = _player.Position - loc.Position;
                    if (awayFromLoc != Vector2.Zero) awayFromLoc.Normalize();
                    Context.Player.Position = loc.Position + awayFromLoc * 500f;

                    // Eagerly generate the town now so we can find the gate spawn position
                    if (!Context.Town.Locations.Any(l => l.SceneId == loc.Id))
                    {
                        JumpAndRun.WorldGen.TownGenerator.GenerateTown(loc.Id, _seed, Context);
                    }

                    // Spawn the player near the first gate portal inside the town
                    var gatePortal = Context.Town.Locations
                        .FirstOrDefault(l => l.SceneId == loc.Id && l.IsPortal);
                    Context.Player.TownPosition = gatePortal != null
                        ? gatePortal.Position + new Vector2(50, 50) // Just inside the gate
                        : new Vector2(1500, 1500); // Fallback: town center

                    // Transition to the local 'zoomed in' scene
                    var townScene = SceneFactory.Create(loc.Id);
                    SceneManager.Instance.LoadScene(townScene);
                    return; // Exit Update
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _graphicsDevice.Clear(Color.Black);

            // Apply Camera Transform to BasicEffect
            _basicEffect.View = _camera.Transform;

            if (_vertices.Count > 0 && _indices.Count > 0)
            {
                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertices.ToArray(),
                        0,
                        _vertices.Count,
                        _indices.ToArray(),
                        0,
                        _indices.Count / 3
                    );
                }
            }

            // Draw roads
            if (_roadVertices.Count > 0)
            {
                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        _roadVertices.ToArray(),
                        0,
                        _roadVertices.Count / 2
                    );
                }
            }

            // Draw polygon borders
            if (_borderVertices.Count > 0)
            {
                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        _borderVertices.ToArray(),
                        0,
                        _borderVertices.Count / 2
                    );
                }
            }

            // Draw rivers via lines (PrimitiveType.LineList)
            // ...

            // Draw props (Vegetation/Rocks)
            spriteBatch.Begin(transformMatrix: _camera.Transform);
            foreach (var prop in Context.Props)
            {
                Color propColor = prop.Type switch
                {
                    PropType.Tree => Color.DarkGreen,
                    PropType.PineTree => new Color(34, 139, 34), // Forest Green
                    PropType.Bush => Color.LightGreen,
                    PropType.Cactus => Color.OliveDrab,
                    PropType.Rock => Color.DarkGray,
                    _ => Color.Pink
                };
                
                // Increase prop size realistically to match new proportions
                int size = (int)(40f * prop.Size);
                spriteBatch.Draw(_pixel, new Rectangle((int)prop.Position.X - size/2, (int)prop.Position.Y - size/2, size, size), propColor);
            }

            // Draw locations — box sized to match 300px collision radius (diameter = 600)
            foreach (var loc in Context.Town.Locations.Where(l => l.SceneId == "OpenWorldScene"))
            {
                int boxSize = 600;
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - boxSize/2, (int)loc.Position.Y - boxSize/2, boxSize, boxSize), Color.Red * 0.4f);
                // Bright border: draw 4 thin edge lines by layering smaller rects
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - boxSize/2, (int)loc.Position.Y - boxSize/2, boxSize, 3), Color.Red);
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - boxSize/2, (int)loc.Position.Y + boxSize/2 - 3, boxSize, 3), Color.Red);
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - boxSize/2, (int)loc.Position.Y - boxSize/2, 3, boxSize), Color.Red);
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X + boxSize/2 - 3, (int)loc.Position.Y - boxSize/2, 3, boxSize), Color.Red);
                DebugFont.DrawString(spriteBatch, loc.Name, loc.Position + new Vector2(-boxSize/2f + 5, -boxSize/2f - 15), Color.White);
            }

            // Draw player
            _player.Draw(spriteBatch);

            spriteBatch.End();

            // UI Layer
            spriteBatch.Begin();
            string debugText = $"OpenWorld MapGen Visualizer - Seed: {_seed} | Shape: {_currentShape}\n" +
                               $"Vertices: {_vertices.Count} Indices: {_indices.Count} Zoom: {_camera.Zoom:F1}x\n" +
                               $"Press 'R' for new map, 'T' to change shape";
            DebugFont.DrawString(spriteBatch, debugText, new Vector2(10, 10), Color.White);
            spriteBatch.End();
        }
    }
}
