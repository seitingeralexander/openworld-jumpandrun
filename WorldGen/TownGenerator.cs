using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JumpAndRun.World;
using JumpAndRun.Core;
using System.Linq;

namespace JumpAndRun.WorldGen
{
    public static class TownGenerator
    {
        public static void GenerateTown(string townId, int globalSeed, SimContext context)
        {
            int townSeed = globalSeed ^ townId.GetHashCode();
            Random rng = new Random(townSeed);

            Console.WriteLine($"[TownGenerator] Generating Voronoi layout for town: {townId} with seed: {townSeed}");

            IslandShapeType shape = (IslandShapeType)rng.Next(0, 4); 
            // Create a larger map generator specifically for this town instance (300x300 real world meters = 3000x3000 px)
            var mapGen = new MapGenerator(townSeed, variant: 0, shape, numPoints: 400, width: 3000, height: 3000, MapMode.Town);
            mapGen.Generate();
            
            // Save the generator so TownScene can render its polygons
            context.Town.TownMaps[townId] = mapGen;

            // 1. Place the "Exit to Map" portals exactly on the wall gates
            var gateCenters = mapGen.Centers
                .Where(c => c.District != DistrictType.Wilderness && c.Borders.Any(b => b.IsWall))
                .OrderBy(c => rng.Next())
                .Take(2) // Up to 2 exits
                .ToList();

            if (gateCenters.Count == 0) // Fallback if perfectly circular
                gateCenters = mapGen.Centers.Where(c => c.District != DistrictType.Wilderness).Take(1).ToList();

            for (int i = 0; i < gateCenters.Count; i++)
            {
                var gateCenter = gateCenters[i];
                var exitPortal = new Location($"{townId}_exit_{i}", "Town Gate", gateCenter.Point, LocationType.Portal)
                {
                    TargetSceneId = "OpenWorldScene",
                    SceneId = townId
                };
                context.Town.AddLocation(exitPortal);
                // Mark this center as a portal so we don't spawn a house here
                gateCenter.District = DistrictType.None; 
            }

            // 2. Spawn buildings based on Districts
            foreach (var center in mapGen.Centers)
            {
                if (center.District == DistrictType.Wilderness || center.District == DistrictType.None) continue;

                // Pick a building type based on District
                LocationType type = LocationType.Home; // default
                switch (center.District)
                {
                    case DistrictType.Market: type = rng.Next(10) < 7 ? LocationType.Service : LocationType.Leisure; break;
                    case DistrictType.Noble: type = LocationType.Home; break; // Maybe larger capacity?
                    case DistrictType.Residential: type = rng.Next(10) < 8 ? LocationType.Home : LocationType.Work; break;
                    case DistrictType.Slum: type = rng.Next(10) < 9 ? LocationType.Home : LocationType.Service; break;
                    case DistrictType.Farm: type = LocationType.Work; break;
                    case DistrictType.Military: type = LocationType.Work; break; // Guardhouse
                }

                // 25% chance to skip and leave open space
                if (rng.NextDouble() < 0.25) continue;

                // Prevent overlapping buildings
                bool isOverlapping = false;
                foreach (var existingLoc in context.Town.Locations.Where(l => l.SceneId == townId))
                {
                    // Require at least 80 pixels between centers to prevent the 70x70 bounding boxes from overlapping
                    if (Vector2.DistanceSquared(existingLoc.Position, center.Point) < 6400f) 
                    {
                        isOverlapping = true;
                        break;
                    }
                }
                if (isOverlapping) continue;

                string name = GenerateLocationName(type, rng);
                string id = $"{townId}_{type.ToString().ToLower()}_{center.Index}_{rng.Next(1000)}";

                var loc = new Location(id, name, center.Point, type)
                {
                    Capacity = center.District == DistrictType.Noble ? rng.Next(10, 20) : rng.Next(3, 10),
                    SceneId = townId
                };

                // Basic needs setup
                if (type == LocationType.Home) loc.SetNeedRate(Simulation.NeedType.Energy, 10f);
                else if (type == LocationType.Leisure) loc.SetNeedRate(Simulation.NeedType.Social, 15f);
                else if (type == LocationType.Service) loc.SetNeedRate(Simulation.NeedType.Hunger, 20f);
                else if (type == LocationType.Work) loc.SetNeedRate(Simulation.NeedType.Energy, -2f);

                context.Town.AddLocation(loc);
            }

            // 3. Generate Streets with a single Dijkstra flood from the Market hub
            // This marks shortest-path edges outward from the hub to gates, residential, and slum areas.
            Console.WriteLine($"[TownGenerator] Generating street network...");
            
            var markets = mapGen.Centers.Where(c => c.District == DistrictType.Market && !c.Ocean && !c.Water).ToList();
            if (markets.Count == 0) 
                markets = mapGen.Centers.Where(c => c.District != DistrictType.Wilderness && !c.Ocean && !c.Water).Take(1).ToList();
            
            if (markets.Count > 0)
            {
                var mainHub = markets[0];

                // Collect targets: gates + sample of residential + sample of slum
                var targets = new HashSet<Center>(gateCenters);
                mapGen.Centers.Where(c => c.District == DistrictType.Residential && !c.Ocean).OrderBy(_ => rng.Next()).Take(5).ToList().ForEach(c => targets.Add(c));
                mapGen.Centers.Where(c => c.District == DistrictType.Slum && !c.Ocean).OrderBy(_ => rng.Next()).Take(3).ToList().ForEach(c => targets.Add(c));

                mapGen.DijkstraMarkRoads(mainHub, targets);
            }
        }

        private static string GenerateLocationName(LocationType type, Random rng)
        {
            string[] homePrefixes = { "Cozy", "Old", "Sturdy", "Wooden", "Stone" };
            string[] workNames = { "Blacksmith", "Farm", "Lumber Mill", "Mine", "Trading Post" };
            string[] serviceNames = { "Market", "General Store", "Apothecary", "Bakery" };
            string[] leisureNames = { "Tavern", "Inn", "Town Square", "Park" };

            return type switch
            {
                LocationType.Home => $"{homePrefixes[rng.Next(homePrefixes.Length)]} House",
                LocationType.Work => workNames[rng.Next(workNames.Length)],
                LocationType.Service => serviceNames[rng.Next(serviceNames.Length)],
                LocationType.Leisure => leisureNames[rng.Next(leisureNames.Length)],
                _ => "Unknown Building"
            };
        }
    }
}
