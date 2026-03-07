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
        private int _seed = 42;
        private IslandShapeType _currentShape = IslandShapeType.Radial;
        
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

            GenerateWorld();
        }

        private void GenerateWorld()
        {
            _mapGen = new MapGenerator(seed: _seed, variant: _seed, shape: _currentShape, numPoints: 1000, width: 2000, height: 2000);
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
                _vertices.Add(new VertexPositionColor(new Vector3(center.Point, 0), color));

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

                    // Draw Rivers if any
                    // Note: We'll draw them via a different method (Lines) or just bake them into colors

                    _vertices.Add(new VertexPositionColor(new Vector3(current.Point, 0), color));
                    
                    _indices.Add(startIndex);
                    _indices.Add(_vertices.Count - 1);
                    _indices.Add((i + 1 == sortedCorners.Count) ? startIndex + 1 : _vertices.Count);
                    
                    // Add border lines
                    _borderVertices.Add(new VertexPositionColor(new Vector3(current.Point, 0), Color.DarkGray * 0.5f));
                    _borderVertices.Add(new VertexPositionColor(new Vector3(next.Point, 0), Color.DarkGray * 0.5f));
                }
            }

            // Generate Roads
            Color roadColor = new Color(74, 55, 40); // Dark brown dirt road color
            foreach (var edge in _mapGen.Edges)
            {
                if (edge.Road > 0)
                {
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D0.Point, 0), roadColor));
                    _roadVertices.Add(new VertexPositionColor(new Vector3(edge.D1.Point, 0), roadColor));
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
            // Camera controls
            var kbd = InputManager.Instance;
            float camSpeed = 500f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (kbd.IsKeyDown(Keys.W)) _cameraPosition += new Vector2(0, -camSpeed);
            if (kbd.IsKeyDown(Keys.S)) _cameraPosition += new Vector2(0, camSpeed);
            if (kbd.IsKeyDown(Keys.A)) _cameraPosition += new Vector2(-camSpeed, 0);
            if (kbd.IsKeyDown(Keys.D)) _cameraPosition += new Vector2(camSpeed, 0);

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
            _camera.Follow(_cameraPosition);
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
                
                int size = (int)(4 * prop.Size);
                spriteBatch.Draw(_pixel, new Rectangle((int)prop.Position.X - size/2, (int)prop.Position.Y - size/2, size, size), propColor);
            }

            // Draw locations
            foreach (var loc in Context.Town.Locations)
            {
                spriteBatch.Draw(_pixel, new Rectangle((int)loc.Position.X - 5, (int)loc.Position.Y - 5, 10, 10), Color.Red);
                DebugFont.DrawString(spriteBatch, loc.Name, loc.Position + new Vector2(10, -10), Color.White);
            }
            spriteBatch.End();

            spriteBatch.Begin();
            string debugText = $"OpenWorld MapGen Visualizer - Seed: {_seed} | Shape: {_currentShape}\n" +
                               $"Vertices: {_vertices.Count} Indices: {_indices.Count} Zoom: {_camera.Zoom:F1}x\n" +
                               $"Press 'R' for new map, 'T' to change shape";
            DebugFont.DrawString(spriteBatch, debugText, new Vector2(10, 10), Color.White);
            spriteBatch.End();
        }
    }
}
