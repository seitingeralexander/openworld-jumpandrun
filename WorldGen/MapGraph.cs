using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace JumpAndRun.WorldGen
{
    public class Center
    {
        public int Index { get; set; }
        public Vector2 Point { get; set; } // Location
        public bool Water { get; set; } // Lake or ocean
        public bool Ocean { get; set; } // Ocean
        public bool Coast { get; set; } // Land polygon touching an ocean
        public bool Border { get; set; } // At the edge of the map
        public string Biome { get; set; } // Biome type
        public float Elevation { get; set; } // 0.0-1.0
        public float Moisture { get; set; } // 0.0-1.0

        public List<Center> Neighbors { get; set; } = new List<Center>();
        public List<Edge> Borders { get; set; } = new List<Edge>();
        public List<Corner> Corners { get; set; } = new List<Corner>();
    }

    public class Corner
    {
        public int Index { get; set; }
        public Vector2 Point { get; set; } // Location
        public bool Ocean { get; set; } // Ocean
        public bool Water { get; set; } // Lake or ocean
        public bool Coast { get; set; } // Touches ocean and land polygons
        public bool Border { get; set; } // At the edge of the map
        public float Elevation { get; set; } // 0.0-1.0
        public float Moisture { get; set; } // 0.0-1.0
        public int River { get; set; } // 0 if no river, or volume of water in river
        public Corner Downslope { get; set; } // Pointer to adjacent corner most downhill
        public Corner Watershed { get; set; } // Pointer to coastal corner, or null
        public int WatershedSize { get; set; }

        public List<Center> Touches { get; set; } = new List<Center>(); // Polygons touching this corner
        public List<Edge> Protrudes { get; set; } = new List<Edge>(); // Edges touching this corner
        public List<Corner> Adjacent { get; set; } = new List<Corner>(); // Corners connected by edges
    }

    public class Edge
    {
        public int Index { get; set; }
        public Center D0 { get; set; } // Delaunay edge
        public Center D1 { get; set; }
        public Corner V0 { get; set; } // Voronoi edge
        public Corner V1 { get; set; }
        public Vector2 Midpoint { get; set; } // Halfway between v0, v1
        public int River { get; set; } // Volume of water, or 0
        public int Road { get; set; } // 0 if no road, 1 or higher based on road type connecting centers
    }
}
