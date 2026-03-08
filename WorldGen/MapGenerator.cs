using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MIConvexHull;

namespace JumpAndRun.WorldGen
{
    // MIConvexHull required interfaces
    public class MapVertex : IVertex
    {
        public double[] Position { get; set; }
        public Center CenterRef { get; set; }
        
        public MapVertex(double x, double y)
        {
            Position = new double[] { x, y };
        }
    }

    public class MapCell : TriangulationCell<MapVertex, MapCell>
    {
        public Corner CornerRef { get; set; }

        public Vector2 GetCircumcenter()
        {
            // Calculate circumcenter of the triangle formed by the 3 vertices
            var a = Vertices[0].Position;
            var b = Vertices[1].Position;
            var c = Vertices[2].Position;

            double d = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]));
            if (Math.Abs(d) < 1e-6) return new Vector2((float)a[0], (float)a[1]); // Collinear fallback

            double ux = ((a[0] * a[0] + a[1] * a[1]) * (b[1] - c[1]) + (b[0] * b[0] + b[1] * b[1]) * (c[1] - a[1]) + (c[0] * c[0] + c[1] * c[1]) * (a[1] - b[1])) / d;
            double uy = ((a[0] * a[0] + a[1] * a[1]) * (c[0] - b[0]) + (b[0] * b[0] + b[1] * b[1]) * (a[0] - c[0]) + (c[0] * c[0] + c[1] * c[1]) * (b[0] - a[0])) / d;

            return new Vector2((float)ux, (float)uy);
        }
    }

    public enum IslandShapeType
    {
        Radial,
        Perlin,
        Square,
        Blob
    }

    public enum MapMode
    {
        World,
        Town
    }

    public class MapGenerator
    {
        public List<Center> Centers { get; private set; } = new();
        public List<Corner> Corners { get; private set; } = new();
        public List<Edge> Edges { get; private set; } = new();

        private int _seed;
        private int _numPoints;
        private int _variant;
        private Rectangle _bounds;
        private Random _random;
        private IslandShapeType _islandShape;
        private MapMode _mode;

        public MapGenerator(int seed = 42, int variant = 0, IslandShapeType shape = IslandShapeType.Radial, int numPoints = 1000, int width = 1000, int height = 1000, MapMode mode = MapMode.World)
        {
            _seed = seed;
            _variant = variant;
            _islandShape = shape;
            _numPoints = numPoints;
            _mode = mode;
            _bounds = new Rectangle(0, 0, width, height);
            _random = new Random(_variant); // The map topology uses the variant seed
        }

        public void Generate()
        {
            // 1. Generate points with slight relaxation
            var points = GeneratePoints();
            
            // 2. Build Graph (Centers, Corners, Edges)
            BuildGraph(points);

            // 3. Assign Elevations and Coastlines
            AssignElevations();
            AssignOceanCoastAndLand();
            RedistributeElevations();
            AssignPolygonElevations();

            if (_mode == MapMode.World)
            {
                // 4. Calculate Downsopes
                CalculateDownslopes();

                // 5. Generate Rivers
                GenerateRivers(Math.Min(100, _numPoints / 10));

                // 6. Assign Moisture
                AssignMoisture();
                RedistributeMoisture();

                // 7. Assign Biomes
                AssignBiomes();
            }
            else
            {
                // Town Mode: Assign Districts
                AssignTownDistricts();
            }
        }

        private void AssignTownDistricts()
        {
            Vector2 mapCenter = new Vector2(_bounds.Width / 2f, _bounds.Height / 2f);
            
            // 1. Calculate max distance of any land to the center to normalize the distances
            float maxLandDist = 1f; 
            foreach (var center in Centers.Where(c => !c.Ocean && !c.Water))
            {
                float dist = Vector2.Distance(center.Point, mapCenter);
                if (dist > maxLandDist) maxLandDist = dist;
            }

            // 2. Assign Districts
            foreach (var center in Centers)
            {
                if (center.Ocean || center.Water)
                {
                    center.District = DistrictType.Wilderness;
                    continue;
                }

                float normalizedDist = Vector2.Distance(center.Point, mapCenter) / maxLandDist;
                
                // Add some noise to the assignment
                double noise = (_random.NextDouble() - 0.5) * 0.2;
                float adjustedDist = MathHelper.Clamp(normalizedDist + (float)noise, 0f, 1f);

                if (adjustedDist < 0.2f) center.District = DistrictType.Market;
                else if (adjustedDist < 0.4f) center.District = DistrictType.Noble;
                else if (adjustedDist < 0.7f) center.District = DistrictType.Residential;
                else if (adjustedDist < 0.85f) center.District = DistrictType.Slum;
                else center.District = DistrictType.Farm;
            }

            // 3. Assign Town Walls
            foreach (var edge in Edges)
            {
                if (edge.D0 != null && edge.D1 != null)
                {
                    bool d0Wild = edge.D0.District == DistrictType.Wilderness;
                    bool d1Wild = edge.D1.District == DistrictType.Wilderness;

                    // Wall is on the edge between Wilderness and non-Wilderness
                    if (d0Wild != d1Wild)
                    {
                        edge.IsWall = true;
                    }
                }
            }
        }

        private List<MapVertex> GeneratePoints()
        {
            // AmitP point selector uses seed for shapes, but we'll use _seed for points and _variant for properties
            var pointRandom = new Random(_seed);
            List<MapVertex> points = new();
            for (int i = 0; i < _numPoints; i++)
            {
                // Leave a little margin (10 units) from the absolute edges
                double x = 10 + pointRandom.NextDouble() * (_bounds.Width - 20);
                double y = 10 + pointRandom.NextDouble() * (_bounds.Height - 20);
                points.Add(new MapVertex(x, y));
            }
            return points;
        }

        private void BuildGraph(List<MapVertex> points)
        {
            // Create Delaunay Triangulation
            var delaunay = VoronoiMesh.Create<MapVertex, MapCell>(points);

            // Create Centers (1 per input point/polygon)
            Dictionary<MapVertex, Center> vertexToCenter = new();
            foreach (var v in points)
            {
                var center = new Center
                {
                    Index = Centers.Count,
                    Point = new Vector2((float)v.Position[0], (float)v.Position[1])
                };
                Centers.Add(center);
                v.CenterRef = center;
                vertexToCenter[v] = center;
            }

            // Create Corners (1 per Delaunay triangle = Voronoi vertex)
            foreach (var cell in delaunay.Vertices) // In MIConvexHull, Vertices of VoronoiMesh are the Delaunay triangles
            {
                var corner = new Corner
                {
                    Index = Corners.Count,
                    Point = cell.GetCircumcenter()
                };
                Corners.Add(corner);
                cell.CornerRef = corner;

                // Link Centers to this Corner, and Corner to Centers
                foreach (var v in cell.Vertices)
                {
                    var center = v.CenterRef;
                    corner.Touches.Add(center);
                    if (!center.Corners.Contains(corner))
                    {
                        center.Corners.Add(corner);
                    }
                }
            }

            // Create Edges
            // Iterate over all Delaunay cells to build edges
            var edgeCache = new HashSet<string>();
            foreach (var cell in delaunay.Vertices)
            {
                for (int i = 0; i < 3; i++)
                {
                    var neighborCell = cell.Adjacency[i];
                    if (neighborCell != null)
                    {
                        var v0 = cell.CornerRef;
                        var v1 = neighborCell.CornerRef;

                        // Create a unique key for the edge
                        string key = v0.Index < v1.Index ? $"{v0.Index}_{v1.Index}" : $"{v1.Index}_{v0.Index}";
                        if (!edgeCache.Contains(key))
                        {
                            edgeCache.Add(key);

                            // The two centers are the vertices shared by these two cells
                            var sharedCenters = cell.Vertices.Intersect(neighborCell.Vertices).Select(v => v.CenterRef).ToList();
                            
                            if (sharedCenters.Count == 2)
                            {
                                var d0 = sharedCenters[0];
                                var d1 = sharedCenters[1];

                                var edge = new Edge
                                {
                                    Index = Edges.Count,
                                    V0 = v0,
                                    V1 = v1,
                                    D0 = d0,
                                    D1 = d1,
                                    Midpoint = (v0.Point + v1.Point) / 2f
                                };

                                Edges.Add(edge);

                                // Append edge to corners
                                v0.Protrudes.Add(edge);
                                v1.Protrudes.Add(edge);
                                v0.Adjacent.Add(v1);
                                v1.Adjacent.Add(v0);

                                // Append edge to centers
                                d0.Borders.Add(edge);
                                d1.Borders.Add(edge);
                                d0.Neighbors.Add(d1);
                                d1.Neighbors.Add(d0);
                            }
                        }
                    }
                }
            }

            // Mark border corners and centers
            foreach (var corner in Corners)
            {
                // A corner is on the border if it's near the edge of the screen
                if (corner.Point.X <= 0 || corner.Point.X >= _bounds.Width ||
                    corner.Point.Y <= 0 || corner.Point.Y >= _bounds.Height)
                {
                    corner.Border = true;
                    foreach (var center in corner.Touches)
                    {
                        center.Border = true;
                    }
                }
            }

            // Also mark centers that don't have enough neighbors
            foreach (var center in Centers)
            {
                if (center.Neighbors.Count < 3)
                {
                    center.Border = true;
                }
            }
        }

        private void AssignElevations()
        {
            foreach (var corner in Corners)
            {
                corner.Water = !Inside(corner.Point);
            }

            Queue<Corner> queue = new Queue<Corner>();
            
            foreach (var corner in Corners)
            {
                if (corner.Border) 
                {
                    corner.Elevation = 0f;
                    queue.Enqueue(corner);
                }
                else
                {
                    corner.Elevation = float.PositiveInfinity;
                }
            }

            while (queue.Count > 0)
            {
                var q = queue.Dequeue();
                foreach (var s in q.Adjacent)
                {
                    float newElevation = 0.01f + q.Elevation;
                    if (!q.Water && !s.Water)
                    {
                        newElevation += 1f;
                    }
                    if (newElevation < s.Elevation)
                    {
                        s.Elevation = newElevation;
                        queue.Enqueue(s);
                    }
                }
            }
        }

        private void AssignOceanCoastAndLand()
        {
            Queue<Center> queue = new Queue<Center>();
            float LAKE_THRESHOLD = 0.3f;

            foreach (var p in Centers)
            {
                int numWater = 0;
                foreach (var q in p.Corners)
                {
                    if (q.Border)
                    {
                        p.Border = true;
                        p.Ocean = true;
                        q.Water = true;
                        queue.Enqueue(p);
                    }
                    if (q.Water)
                    {
                        numWater++;
                    }
                }
                p.Water = (p.Ocean || numWater >= p.Corners.Count * LAKE_THRESHOLD);
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var r in p.Neighbors)
                {
                    if (r.Water && !r.Ocean)
                    {
                        r.Ocean = true;
                        queue.Enqueue(r);
                    }
                }
            }

            foreach (var p in Centers)
            {
                int numOcean = 0;
                int numLand = 0;
                foreach (var r in p.Neighbors)
                {
                    if (r.Ocean) numOcean++;
                    if (!r.Water) numLand++;
                }
                p.Coast = (numOcean > 0) && (numLand > 0);
            }

            foreach (var q in Corners)
            {
                int numOcean = 0;
                int numLand = 0;
                foreach (var p in q.Touches)
                {
                    if (p.Ocean) numOcean++;
                    if (!p.Water) numLand++;
                }
                q.Ocean = (numOcean == q.Touches.Count);
                q.Coast = (numOcean > 0) && (numLand > 0);
                q.Water = q.Border || ((numLand != q.Touches.Count) && !q.Coast);
            }
        }

        private void RedistributeElevations()
        {
            float SCALE_FACTOR = 1.1f;
            var landCorners = Corners.Where(q => !q.Ocean && !q.Coast).OrderBy(q => q.Elevation).ToList();

            if (landCorners.Count > 0)
            {
                for (int i = 0; i < landCorners.Count; i++)
                {
                    float y = (float)i / (landCorners.Count - 1);
                    float x = (float)(Math.Sqrt(SCALE_FACTOR) - Math.Sqrt(SCALE_FACTOR * (1 - y)));
                    if (x > 1.0f) x = 1.0f;
                    landCorners[i].Elevation = x;
                }
            }

            foreach (var q in Corners)
            {
                if (q.Ocean || q.Coast)
                {
                    q.Elevation = 0.0f;
                }
            }
        }

        private void AssignPolygonElevations()
        {
            foreach (var p in Centers)
            {
                float sumElevation = 0.0f;
                foreach (var q in p.Corners)
                {
                    sumElevation += q.Elevation;
                }
                p.Elevation = sumElevation / p.Corners.Count;
            }
        }

        private void CalculateDownslopes()
        {
            foreach (var corner in Corners)
            {
                var downslope = corner;
                foreach (var adj in corner.Adjacent)
                {
                    if (adj.Elevation <= downslope.Elevation)
                    {
                        downslope = adj;
                    }
                }
                corner.Downslope = downslope;
            }
        }

        private void GenerateRivers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Pick a random corner that is land and has high elevation
                var corner = Corners[_random.Next(Corners.Count)];
                if (corner.Ocean || corner.Elevation < 0.5f) continue;

                while (!corner.Coast && corner.Downslope != corner)
                {
                    var edge = corner.Protrudes.FirstOrDefault(e => e.V0 == corner.Downslope || e.V1 == corner.Downslope);
                    if (edge != null)
                    {
                        edge.River++;
                    }
                    corner.River++;
                    corner = corner.Downslope;
                }
            }
        }

        private void AssignMoisture()
        {
            // Seed moisture at rivers and lakes
            Queue<Corner> queue = new Queue<Corner>();
            foreach (var corner in Corners)
            {
                if ((corner.Water || corner.River > 0) && !corner.Ocean)
                {
                    corner.Moisture = corner.River > 0 ? Math.Min(3.0f, 0.2f * corner.River) : 1.0f;
                    queue.Enqueue(corner);
                }
                else
                {
                    corner.Moisture = 0f;
                }
            }

            // Propagate moisture
            while (queue.Count > 0)
            {
                var corner = queue.Dequeue();
                foreach (var adj in corner.Adjacent)
                {
                    float newMoisture = corner.Moisture * 0.9f;
                    if (newMoisture > adj.Moisture)
                    {
                        adj.Moisture = newMoisture;
                        queue.Enqueue(adj);
                    }
                }
            }

            // Average moisture to centers
            foreach (var center in Centers)
            {
                float sum = 0f;
                foreach (var corner in center.Corners) sum += corner.Moisture;
                center.Moisture = sum / center.Corners.Count;
            }
        }

        private void RedistributeMoisture()
        {
            var landCorners = Corners.Where(q => !q.Ocean && !q.Coast).OrderBy(q => q.Moisture).ToList();
            for (int i = 0; i < landCorners.Count; i++)
            {
                landCorners[i].Moisture = (float)i / (landCorners.Count - 1);
            }

            foreach (var center in Centers)
            {
                if (center.Border || center.Ocean) continue;
                
                float sum = 0f;
                foreach (var corner in center.Corners)
                {
                    sum += corner.Moisture;
                }
                center.Moisture = sum / center.Corners.Count;
            }
        }

        private void AssignBiomes()
        {
            foreach (var center in Centers)
            {
                if (center.Ocean)
                {
                    center.Biome = "Ocean";
                }
                else if (center.Water)
                {
                    center.Biome = "Lake";
                }
                else if (center.Coast)
                {
                    center.Biome = "Beach";
                }
                else if (center.Elevation > 0.8f)
                {
                    if (center.Moisture > 0.5f) center.Biome = "Snow";
                    else if (center.Moisture > 0.33f) center.Biome = "Tundra";
                    else if (center.Moisture > 0.16f) center.Biome = "Bare";
                    else center.Biome = "Scorched";
                }
                else if (center.Elevation > 0.6f)
                {
                    if (center.Moisture > 0.66f) center.Biome = "Taiga";
                    else if (center.Moisture > 0.33f) center.Biome = "Shrubland";
                    else center.Biome = "TemperateDesert";
                }
                else if (center.Elevation > 0.3f)
                {
                    if (center.Moisture > 0.83f) center.Biome = "TemperateRainForest";
                    else if (center.Moisture > 0.5f) center.Biome = "TemperateDeciduousForest";
                    else if (center.Moisture > 0.16f) center.Biome = "Grassland";
                    else center.Biome = "TemperateDesert";
                }
                else
                {
                    if (center.Moisture > 0.66f) center.Biome = "TropicalRainForest";
                    else center.Biome = "SubtropicalDesert";
                }
            }
        }

        /// <summary>
        /// Single-source Dijkstra from <paramref name="source"/>. When a node in <paramref name="targets"/> 
        /// is settled, it traces back the path and marks those edges as roads. Much faster than 
        /// running separate A* calls for each target.
        /// </summary>
        public void DijkstraMarkRoads(Center source, HashSet<Center> targets)
        {
            var dist = new Dictionary<Center, float> { [source] = 0f };
            var prev = new Dictionary<Center, Center>();
            var settled = new HashSet<Center>();
            var pq = new PriorityQueue<Center, float>();
            pq.Enqueue(source, 0f);

            // How many targets remain so we can early-exit
            int remaining = targets.Count;

            while (pq.Count > 0 && remaining > 0)
            {
                var u = pq.Dequeue();
                if (!settled.Add(u)) continue;

                // If u is a target, trace back and mark its path
                if (targets.Contains(u))
                {
                    var cur = u;
                    while (prev.TryGetValue(cur, out var p))
                    {
                        var edge = GetEdge(cur, p);
                        if (edge != null) edge.Road = 1;
                        cur = p;
                    }
                    remaining--;
                }

                float uDist = dist.GetValueOrDefault(u, float.PositiveInfinity);
                foreach (var v in u.Neighbors)
                {
                    if (settled.Contains(v)) continue;

                    float edgeCost = Vector2.Distance(u.Point, v.Point);
                    // In Town maps there's no ocean/water, so no massive penalties needed
                    float tentative = uDist + edgeCost;

                    if (tentative < dist.GetValueOrDefault(v, float.PositiveInfinity))
                    {
                        dist[v] = tentative;
                        prev[v] = u;
                        pq.Enqueue(v, tentative);
                    }
                }
            }
        }

        public void BuildRoadAStar(Center start, Center end)
        {
            var openSet = new PriorityQueue<Center, float>();
            var cameFrom = new Dictionary<Center, Center>();
            // Lazy gScore: only allocate entries as we visit nodes (avoids O(n) init per call)
            var gScore = new Dictionary<Center, float> { [start] = 0f };
            // Closed set: skip nodes we already settled optimally
            var closedSet = new HashSet<Center>();

            openSet.Enqueue(start, Heuristic(start, end));

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current == end)
                {
                    ReconstructPath(cameFrom, current);
                    return;
                }

                // Skip if already settled
                if (!closedSet.Add(current)) continue;

                foreach (var neighbor in current.Neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    // Cost function: base distance + terrain penalties
                    float dist = Vector2.Distance(current.Point, neighbor.Point);
                    float cost = dist;

                    if (neighbor.Ocean) cost += 1000000f; // Impassable
                    else if (neighbor.Water) cost += 10000f; // Expensive bridge

                    // For towns (no elevation data), skip the elevation cost
                    float elevDiff = Math.Abs(current.Elevation - neighbor.Elevation);
                    cost += elevDiff * 500f; // Reduced penalty (towns are flat)

                    // Prefer reusing existing roads
                    var edge = GetEdge(current, neighbor);
                    if (edge != null && edge.Road > 0)
                        cost *= 0.1f; // 90% discount

                    float currentG = gScore.GetValueOrDefault(current, float.PositiveInfinity);
                    float tentativeG = currentG + cost;
                    float neighborG = gScore.GetValueOrDefault(neighbor, float.PositiveInfinity);

                    if (tentativeG < neighborG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        openSet.Enqueue(neighbor, tentativeG + Heuristic(neighbor, end));
                    }
                }
            }
        }

        private float Heuristic(Center a, Center b)
        {
            return Vector2.Distance(a.Point, b.Point);
        }

        private void ReconstructPath(Dictionary<Center, Center> cameFrom, Center current)
        {
            while (cameFrom.ContainsKey(current))
            {
                var prev = cameFrom[current];
                var edge = GetEdge(prev, current);
                if (edge != null)
                {
                    edge.Road = 1;
                }
                current = prev;
            }
        }

        private Edge GetEdge(Center a, Center b)
        {
            return a.Borders.FirstOrDefault(e => (e.D0 == a && e.D1 == b) || (e.D1 == a && e.D0 == b));
        }

        private bool Inside(Vector2 point)
        {
            // Normalize point to -1 .. 1
            float nx = 2 * (point.X / _bounds.Width - 0.5f);
            float ny = 2 * (point.Y / _bounds.Height - 0.5f);
            float length = (float)Math.Sqrt(nx * nx + ny * ny);

            switch (_islandShape)
            {
                case IslandShapeType.Radial:
                    {
                        Random shapeRandom = new Random(_seed); // Use original seed
                        int bumps = shapeRandom.Next(1, 6);
                        double startAngle = shapeRandom.NextDouble() * 2 * Math.PI;
                        double dipAngle = shapeRandom.NextDouble() * 2 * Math.PI;
                        double dipWidth = 0.2 + shapeRandom.NextDouble() * 0.5;

                        double angle = Math.Atan2(ny, nx);
                        double len = 0.5 * (Math.Max(Math.Abs(nx), Math.Abs(ny)) + length);

                        double r1 = 0.5 + 0.40 * Math.Sin(startAngle + bumps * angle + Math.Cos((bumps + 3) * angle));
                        double r2 = 0.7 - 0.20 * Math.Sin(startAngle + bumps * angle - Math.Sin((bumps + 2) * angle));
                        if (Math.Abs(angle - dipAngle) < dipWidth ||
                            Math.Abs(angle - dipAngle + 2 * Math.PI) < dipWidth ||
                            Math.Abs(angle - dipAngle - 2 * Math.PI) < dipWidth)
                        {
                            r1 = r2 = 0.2;
                        }

                        double ISLAND_FACTOR = 1.07;
                        return (len < r1 || (len > r1 * ISLAND_FACTOR && len < r2));
                    }
                case IslandShapeType.Square:
                    return true;
                case IslandShapeType.Blob:
                    {
                        bool eye1 = Math.Sqrt(Math.Pow(nx - 0.2, 2) + Math.Pow(ny / 2 + 0.2, 2)) < 0.05;
                        bool eye2 = Math.Sqrt(Math.Pow(nx + 0.2, 2) + Math.Pow(ny / 2 + 0.2, 2)) < 0.05;
                        bool body = length < 0.8 - 0.18 * Math.Sin(5 * Math.Atan2(ny, nx));
                        return body && !eye1 && !eye2;
                    }
                case IslandShapeType.Perlin:
                default:
                    // Fallback simpler perlin approximation since we don't have Flash BitmapData.perlinNoise
                    // We generate a deterministic pseudo-random value based on position
                    // We'll just mimic the perlin condition using sine waves
                    float noise = (float)(Math.Sin(nx * 5 + _seed) * Math.Cos(ny * 5 + _seed) * 0.5f + 0.5f);
                    return noise > (0.3f + 0.3f * length * length);
            }
        }
    }
}
