using Microsoft.Xna.Framework;
using JumpAndRun.World;
using JumpAndRun.Simulation;

namespace JumpAndRun.Core
{
    /// <summary>
    /// Initializes world data (Locations, NPCs, Schedules) into the SimContext.
    /// This is the single source of truth for persistent world state.
    /// </summary>
    public static class WorldDataLoader
    {
        public static void Initialize(SimContext context)
        {
            SetupLocations(context);
            SetupNPCs(context);
        }

        private static void SetupLocations(SimContext context)
        {
            // Home
            var home = new Location("home_01", "Baker's Home", new Vector2(100, 100), LocationType.Home);
            home.SetNeedRate(NeedType.Energy, 10f); // Sleep restores 10/sec

            // Work
            var bakery = new Location("bakery_01", "Bakery", new Vector2(300, 100), LocationType.Work);

            // Service
            var market = new Location("market_01", "Market", new Vector2(300, 300), LocationType.Service);
            market.SetNeedRate(NeedType.Hunger, 20f); // Eating restores 20/sec

            // Leisure
            var tavern = new Location("tavern_01", "Tavern", new Vector2(100, 300), LocationType.Leisure);
            tavern.SetNeedRate(NeedType.Social, 15f);

            context.Town.AddLocation(home);
            context.Town.AddLocation(bakery);
            context.Town.AddLocation(market);
            context.Town.AddLocation(tavern);
        }

        private static void SetupNPCs(SimContext context)
        {
            // Get home location for initial position
            var home = context.Town.GetLocation("home_01");

            // Create Elena the Baker
            var background = new Background("Baker", "Hardworking", "home_01");
            var elena = new NPC("Elena", background);
            elena.Position = home?.Position ?? Vector2.Zero;
            elena.CurrentLocationId = "home_01";

            // Define Elena's daily schedule
            elena.Schedule.AddBlock(new ScheduleBlock(6, 12, ScheduleAction.Work, "bakery_01"));
            elena.Schedule.AddBlock(new ScheduleBlock(12, 13, ScheduleAction.Eat, "market_01"));
            elena.Schedule.AddBlock(new ScheduleBlock(13, 18, ScheduleAction.Work, "bakery_01"));
            elena.Schedule.AddBlock(new ScheduleBlock(18, 22, ScheduleAction.Socialize, "tavern_01"));
            elena.Schedule.AddBlock(new ScheduleBlock(22, 6, ScheduleAction.Sleep, "home_01"));

            context.NPCs.Add(elena);
        }
    }
}
