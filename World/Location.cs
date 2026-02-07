using Microsoft.Xna.Framework;

namespace JumpAndRun.World
{
    public enum LocationType
    {
        Home,
        Work,
        Leisure,
        Service
    }

    public class Location
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public LocationType Type { get; set; }
        public int Capacity { get; set; } = 10;

        public Location(string id, string name, Vector2 position, LocationType type)
        {
            Id = id;
            Name = name;
            Position = position;
            Type = type;
        }
    }
}
