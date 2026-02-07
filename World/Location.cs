using Microsoft.Xna.Framework;
using System.Collections.Generic;
using JumpAndRun.Simulation;

namespace JumpAndRun.World
{
    public enum LocationType
    {
        Home,
        Work,
        Leisure,
        Service,
        Portal
    }

    public class Location
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public LocationType Type { get; set; }
        public int Capacity { get; set; } = 10;
        public Dictionary<NeedType, float> NeedSatisfactionRates { get; private set; }
        
        /// <summary>
        /// Which scene this location belongs to. Defaults to TownScene for backwards compatibility.
        /// </summary>
        public string SceneId { get; set; } = "TownScene";
        
        /// <summary>
        /// Where entities spawn when entering this scene via a portal.
        /// </summary>
        public Vector2 EntryPosition { get; set; }
        
        // Portal properties
        public string TargetSceneId { get; set; } // e.g., "SideScrollScene"
        public bool IsPortal => !string.IsNullOrEmpty(TargetSceneId);

        public Location(string id, string name, Vector2 position, LocationType type)
        {
            Id = id;
            Name = name;
            Position = position;
            Type = type;
            NeedSatisfactionRates = new Dictionary<NeedType, float>();
        }

        public void SetNeedRate(NeedType need, float rate)
        {
            NeedSatisfactionRates[need] = rate;
        }

        public float GetNeedRate(NeedType need)
        {
            return NeedSatisfactionRates.ContainsKey(need) ? NeedSatisfactionRates[need] : 0f;
        }
    }
}
