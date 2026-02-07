using System.Collections.Generic;
using System.Linq;

namespace JumpAndRun.World
{
    public class Town
    {
        public List<Location> Locations { get; private set; }

        public Town()
        {
            Locations = new List<Location>();
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
    }
}
