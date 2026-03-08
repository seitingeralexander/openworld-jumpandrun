using System.Collections.Generic;
using System.Linq;
using JumpAndRun.WorldGen;

namespace JumpAndRun.World
{
    public class Town
    {
        public List<Location> Locations { get; private set; }
        public Dictionary<string, MapGenerator> TownMaps { get; private set; }

        public Town()
        {
            Locations = new List<Location>();
            TownMaps = new Dictionary<string, MapGenerator>();
        }

        public void AddLocation(Location location)
        {
            Locations.Add(location);
        }

        public Location GetLocation(string id)
        {
            return Locations.FirstOrDefault(l => l.Id == id);
        }

        public List<Location> GetLocationsByType(LocationType type)
        {
            return Locations.Where(l => l.Type == type).ToList();
        }

        public Location GetBestLocationForNeed(JumpAndRun.Simulation.NeedType need)
        {
            // Simple logic: Find first location that satisfies the need
            // Future: Find closest, or highest rate
            return Locations.FirstOrDefault(l => l.GetNeedRate(need) > 0);
        }
    }
}
