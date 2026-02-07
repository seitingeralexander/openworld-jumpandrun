using JumpAndRun.Core;
using JumpAndRun.World;
using System.Linq;
using Microsoft.Xna.Framework;

namespace JumpAndRun.Simulation
{
    public class NPCSystem
    {
        private SimContext _context;

        public NPCSystem(SimContext context)
        {
            _context = context;
        }

        public void Update(GameTime gameTime)
        {
            // We can update simulation less frequently than frame rate if needed
            // For now, let's update every frame but scale by time
            // Or better, hook into TimeSystem events? 
            // The user suggested "Every X minutes"
            // Let's stick to Update(GameTime) for continuous need decay, 
            // and check schedule changes on TimeSystem events.
            
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Calculate game time delta based on TimeSystem scaling
            // RealSecondsPerGameMinute = 0.1f implies 1 game minute = 0.1 real seconds.
            // So 1 real second = 10 game minutes.
            // Decay should be per game hour/minute.
            
            // Let's just pass the raw delta and handle scaling in the logic or 
            // use a fixed time step logic aligned with TimeSystem.
            
            foreach (var npc in _context.NPCs)
            {
                UpdateNPC(npc, dt);
            }
        }

        private void UpdateNPC(NPC npc, float dt)
        {
            // 1. Decay Needs
            DecayNeeds(npc, dt);

            // 2. Check Critical Needs
            if (HasCriticalNeeds(npc))
            {
                ResolveCriticalNeeds(npc);
            }
            else
            {
                // 3. Follow Schedule
                FollowSchedule(npc);
            }
        }

        private void DecayNeeds(NPC npc, float dt)
        {
            // Example decay rates (per real second, scaled to game time?)
            // If 1 real sec = 10 game mins.
            // Hunger: 0 to 100. Starves in 3 game days?
            // Let's keep it simple for now: constant decay.
            
            // We need game-time delta. 
            // If TimeSystem updates discrete minutes, maybe we should decay on MinuteChanged event?
            // Continuous decay is smoother for UI bars though.
            
            // Let's assume dt is real seconds.
            // Decay rate should be configured.
            
            float decayRate = 2.0f * dt; // Arbitrary testing value
            npc.Needs.Modify(NeedType.Hunger, -decayRate);
            npc.Needs.Modify(NeedType.Energy, -decayRate * 0.5f);
            
            // Work drain?
            if (npc.State == NPCState.Interacting) // working
            {
                 npc.Needs.Modify(NeedType.Energy, -decayRate);
            }
        }

        private bool HasCriticalNeeds(NPC npc)
        {
            return npc.Needs.IsCritical(NeedType.Hunger) || npc.Needs.IsCritical(NeedType.Energy);
        }

        private void ResolveCriticalNeeds(NPC npc)
        {
            // Simple override logic
            if (npc.Needs.IsCritical(NeedType.Energy))
            {
                // Go Home to Sleep
                // Find Home
                var home = _context.Town.GetLocation(npc.Background.HomeLocationId);
                if (home != null)
                {
                    MoveTo(npc, home);
                    if (IsAt(npc, home))
                    {
                        npc.State = NPCState.Sleeping;
                        npc.Needs.Modify(NeedType.Energy, 5.0f); // Recover fast
                    }
                }
            }
            else if (npc.Needs.IsCritical(NeedType.Hunger))
            {
                // Find Food (e.g. Market or Home)
                // For now, assume any "Food" place.
                // Or just Market.
                var market = _context.Town.GetLocationsByType(LocationType.Service).FirstOrDefault(); // Assuming Service includes food logic for now
                if (market != null)
                {
                    MoveTo(npc, market);
                    if (IsAt(npc, market))
                    {
                         npc.State = NPCState.Interacting; // Eating
                         npc.Needs.Modify(NeedType.Hunger, 5.0f);
                    }
                }
            }
        }

        private void FollowSchedule(NPC npc)
        {
            int currentHour = _context.Time.Hour;
            var block = npc.Schedule.GetBlockForHour(currentHour);

            if (block != null)
            {
                // Execute Block
                Location target = null;
                
                if (!string.IsNullOrEmpty(block.TargetLocationId))
                {
                    target = _context.Town.GetLocation(block.TargetLocationId);
                }
                
                if (target != null)
                {
                    MoveTo(npc, target);
                    if (IsAt(npc, target))
                    {
                         // Perform Action
                         SetStateFromAction(npc, block.Action);
                    }
                }
                else
                {
                    // Action without specific location (or location implied e.g. Work -> Job location)
                    // For now, if no location, just set state
                    SetStateFromAction(npc, block.Action);
                }
            }
            else
            {
                npc.State = NPCState.Idle; // Default
            }
        }

        private void MoveTo(NPC npc, Location location)
        {
            // Instant teleport for Phase 1/2 logic test? 
            // User put "Movement & Pathfinding" in Step 9.
            // "Locations are semantic" -> "NPCs move between locations, not random tiles."
            // "npc.MoveTo(targetLocation.Position);"
            
            // Simple movement logic:
            float speed = 100f; // px/sec
             // We need dt here, but I didn't pass it to helper. 
             // Let's just snap for now to verify logic, or add simple lerp if dt available.
             
            // Let's update Position purely for visualization
            Vector2 dir = location.Position - npc.Position;
            if (dir.Length() > 5f)
            {
                dir.Normalize();
                npc.Position += dir * 2.0f; // Arbitrary speed per update tick
                npc.State = NPCState.Moving;
                npc.CurrentLocationId = null; // In transit
            }
            else
            {
                npc.Position = location.Position;
                npc.CurrentLocationId = location.Id; // Arrived
            }
        }

        private bool IsAt(NPC npc, Location location)
        {
            return npc.CurrentLocationId == location.Id;
        }

        private void SetStateFromAction(NPC npc, ScheduleAction action)
        {
             switch (action)
             {
                 case ScheduleAction.Work: npc.State = NPCState.Interacting; break;
                 case ScheduleAction.Sleep: npc.State = NPCState.Sleeping; break;
                 case ScheduleAction.Eat: npc.State = NPCState.Interacting; break;
                 default: npc.State = NPCState.Idle; break;
             }
        }
    }
}
