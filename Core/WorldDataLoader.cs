using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using JumpAndRun.World;
using JumpAndRun.Simulation;

namespace JumpAndRun.Core
{
    /// <summary>
    /// Initializes world data (Locations, NPCs, Schedules) into the SimContext.
    /// Loads from JSON files in Content/Data directory.
    /// </summary>
    public static class WorldDataLoader
    {
        private const string DataPath = "Content/Data";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void Initialize(SimContext context)
        {
            LoadNPCsFromJson(context);
        }

        // ============ NPC Loading ============

        private static void LoadNPCsFromJson(SimContext context)
        {
            var path = Path.Combine(DataPath, "npcs.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"[WorldDataLoader] Required file not found: {path}");
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<NPCListData>(json, JsonOptions);

            if (data?.Npcs == null || data.Npcs.Count == 0)
            {
                throw new InvalidDataException($"[WorldDataLoader] No NPCs found in {path}");
            }

            foreach (var npcData in data.Npcs)
            {
                var bg = npcData.Background;
                var background = new Background(
                    bg?.Job ?? "Unknown",
                    bg?.Personality ?? "Neutral",
                    bg?.HomeLocationId ?? ""
                );

                var npc = new NPC(npcData.Name, background);

                // Set initial position from location
                var initialLocation = context.Town.GetLocation(npcData.InitialLocationId);
                npc.Position = initialLocation?.Position ?? Vector2.Zero;
                npc.CurrentLocationId = npcData.InitialLocationId ?? "";

                // Add schedule blocks
                if (npcData.Schedule != null)
                {
                    foreach (var block in npcData.Schedule)
                    {
                        var action = Enum.TryParse<ScheduleAction>(block.Action, out var act)
                            ? act
                            : ScheduleAction.Idle;

                        npc.Schedule.AddBlock(new ScheduleBlock(
                            block.StartHour,
                            block.EndHour,
                            action,
                            block.TargetLocationId
                        ));
                    }
                }

                context.NPCs.Add(npc);
            }

            Console.WriteLine($"[WorldDataLoader] Loaded {data.Npcs.Count} NPCs from JSON");
        }
    }
}
