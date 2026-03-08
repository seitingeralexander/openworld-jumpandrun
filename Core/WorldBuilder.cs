using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using JumpAndRun.World;
using JumpAndRun.Simulation;
using JumpAndRun.WorldGen;

namespace JumpAndRun.Core
{
    public class WorldBuilder
    {
        private Random _random;

        public WorldBuilder(int seed)
        {
            _random = new Random(seed);
        }

        public void PopulateSimContext(SimContext context, MapGenerator mapGen)
        {
            Console.WriteLine("[WorldBuilder] Populating world from procedural map...");

            // Temporarily clear existing locations (e.g. from JSON) or we can mix them.
            // Let's only clear procedurally generated ones so we don't break TownScene!
            context.Town.Locations.RemoveAll(l => l.SceneId == "OpenWorldScene");
            context.Props.Clear();

            // Find candidates for towns / locations
            // Good candidates: Flat land (Elevation < 0.6, > 0.1), near fresh water or coast
            var candidates = mapGen.Centers.Where(c => 
                !c.Ocean && !c.Water && // Land
                c.Elevation < 0.6f &&   // Not mountains
                (c.Coast || c.Moisture > 0.3f) // Habitable
            ).ToList();

            if (candidates.Count == 0)
            {
                // Fallback to any land
                candidates = mapGen.Centers.Where(c => !c.Ocean && !c.Water).ToList();
            }

            // Shuffle candidates
            candidates = candidates.OrderBy(x => _random.Next()).ToList();

            // Spawn some primary town locations
            int numLocations = Math.Min(10, candidates.Count);
            var locationCenters = new List<Center>();

            string[] names = { "Oakvale", "Riverwood", "Bowerstone", "Falconia", "Oakhaven", "Mistfall", "Sunnydale", "Pinecrest", "Fairview", "Stonebridge" };

            for (int i = 0; i < numLocations; i++)
            {
                var center = candidates[i];
                locationCenters.Add(center);
                var pos = center.Point * 10f; // SCALE 10x to match Open World renderer
                
                var type = i switch
                {
                    0 => LocationType.Home,
                    1 => LocationType.Work,
                    2 => LocationType.Leisure,
                    _ => LocationType.Service
                };

                string name = i < names.Length ? names[i] : $"Location {i}";

                var loc = new Location($"gen_loc_{i}", name, pos, type)
                {
                    Capacity = _random.Next(5, 20),
                    SceneId = "OpenWorldScene" // Make sure they logically belong to the open world
                };

                // Setup basic needs
                if (type == LocationType.Home)
                {
                    loc.SetNeedRate(NeedType.Energy, 10f);
                }
                else if (type == LocationType.Work)
                {
                    // Work maybe reduces energy but fulfills some other need? Leaving empty for now.
                }
                else if (type == LocationType.Leisure)
                {
                    loc.SetNeedRate(NeedType.Social, 5f);
                }
                else if (type == LocationType.Service)
                {
                    loc.SetNeedRate(NeedType.Hunger, 8f);
                }

                context.Town.AddLocation(loc);
                Console.WriteLine($"[WorldBuilder] Spawned {type} at {pos} in {center.Biome}");
            }

            GenerateRoads(mapGen, locationCenters);
            GenerateEnvironmentProps(context, mapGen);

            // TODO: Move existing NPCs to these new locations or spawn new ones
        }

        private void GenerateRoads(MapGenerator mapGen, List<Center> locationCenters)
        {
            Console.WriteLine($"[WorldBuilder] Generating roads between {locationCenters.Count} locations...");
            
            // Minimal Spanning Tree approach or connect each to the previous to form a loop/chain
            if (locationCenters.Count < 2) return;

            // Simple Chain: Connect 0->1, 1->2... n->0
            for (int i = 0; i < locationCenters.Count; i++)
            {
                var start = locationCenters[i];
                var end = locationCenters[(i + 1) % locationCenters.Count];
                mapGen.BuildRoadAStar(start, end);
            }
        }

        private void GenerateEnvironmentProps(SimContext context, MapGenerator mapGen)
        {
            Console.WriteLine("[WorldBuilder] Generating vegetation and environment props...");
            
            foreach (var center in mapGen.Centers)
            {
                if (center.Ocean || center.Water || center.Border) continue;

                // Determine base density and types based on biome
                float density = 0f;
                List<PropType> availableProps = new List<PropType>();

                switch (center.Biome)
                {
                    case "TropicalRainForest":
                    case "TemperateRainForest":
                    case "TemperateDeciduousForest":
                        density = 0.8f;
                        availableProps.Add(PropType.Tree);
                        availableProps.Add(PropType.Bush);
                        break;
                    case "Taiga":
                        density = 0.7f;
                        availableProps.Add(PropType.PineTree);
                        availableProps.Add(PropType.Rock);
                        break;
                    case "Shrubland":
                    case "Grassland":
                        density = 0.2f;
                        availableProps.Add(PropType.Bush);
                        availableProps.Add(PropType.Tree);
                        break;
                    case "SubtropicalDesert":
                    case "TemperateDesert":
                        density = 0.1f;
                        availableProps.Add(PropType.Cactus);
                        availableProps.Add(PropType.Rock);
                        break;
                    case "Snow":
                    case "Tundra":
                    case "Bare":
                    case "Scorched":
                    case "Beach":
                        density = 0.05f;
                        availableProps.Add(PropType.Rock);
                        break;
                }

                if (availableProps.Count == 0 || density <= 0) continue;

                // Area is loosely tied to distance between corners, let's just spawn a fixed max per polygon
                int maxPropsPerPoly = 8;
                int propsToSpawn = (int)(maxPropsPerPoly * density * (_random.NextDouble() * 0.5 + 0.5));

                // Find bounding box of polygon to scatter
                float minX = center.Corners.Min(c => c.Point.X);
                float maxX = center.Corners.Max(c => c.Point.X);
                float minY = center.Corners.Min(c => c.Point.Y);
                float maxY = center.Corners.Max(c => c.Point.Y);

                for (int i = 0; i < propsToSpawn; i++)
                {
                    // Random point roughly inside polygon
                    var corner = center.Corners[_random.Next(center.Corners.Count)];
                    float t = (float)_random.NextDouble();
                    Vector2 basePos = Vector2.Lerp(center.Point, corner.Point, t);
                    Vector2 pos = basePos * 10f; // SCALE 10x to match Open World renderer

                    PropType type = availableProps[_random.Next(availableProps.Count)];
                    float size = (float)(_random.NextDouble() * 0.5 + 0.75); // 0.75x to 1.25x

                    context.Props.Add(new EnvironmentProp(pos, type, size));
                }
            }
        }
    }
}
