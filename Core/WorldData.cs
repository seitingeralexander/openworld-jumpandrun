using System.Collections.Generic;

namespace JumpAndRun.Core
{
    /// <summary>
    /// Data Transfer Objects for JSON deserialization.
    /// These classes mirror the JSON structure and are converted to domain objects.
    /// </summary>

    // ============ Location DTOs ============

    public class WorldData
    {
        public List<LocationData> Locations { get; set; } = new();
    }

    public class LocationData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PositionData Position { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; } = 10;
        public Dictionary<string, float> NeedRates { get; set; } = new();
        public string TargetSceneId { get; set; } // For portal locations
        public string SceneId { get; set; } // Which scene this location belongs to
    }

    public class PositionData
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    // ============ NPC DTOs ============

    public class NPCListData
    {
        public List<NPCData> Npcs { get; set; } = new();
    }

    public class NPCData
    {
        public string Name { get; set; }
        public BackgroundData Background { get; set; }
        public string InitialLocationId { get; set; }
        public List<ScheduleBlockData> Schedule { get; set; } = new();
    }

    public class BackgroundData
    {
        public string Job { get; set; }
        public string Personality { get; set; }
        public string HomeLocationId { get; set; }
    }

    public class ScheduleBlockData
    {
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public string Action { get; set; }
        public string TargetLocationId { get; set; }
    }
}
