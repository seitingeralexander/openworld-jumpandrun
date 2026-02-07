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

            // 1.5 Recover Needs (if at a location)
            RecoverNeeds(npc, dt);

            // 2. Check Critical Needs
            if (HasCriticalNeeds(npc))
            {
                ResolveCriticalNeeds(npc, dt);
            }
            else
            {
                // 3. Follow Schedule
                FollowSchedule(npc, dt);
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
            
            float decayRate = 2.0f * dt; // Arbitrary testing value
            npc.Needs.Modify(NeedType.Hunger, -decayRate);
            npc.Needs.Modify(NeedType.Energy, -decayRate * 0.5f);
            
            // Work drain?
            if (npc.State == NPCState.Interacting) // working
            {
                 npc.Needs.Modify(NeedType.Energy, -decayRate);
            }
        }

        private void RecoverNeeds(NPC npc, float dt)
        {
            if (!string.IsNullOrEmpty(npc.CurrentLocationId))
            {
                var loc = _context.Town.GetLocation(npc.CurrentLocationId);
                if (loc != null)
                {
                    foreach (var kvp in loc.NeedSatisfactionRates)
                    {
                        npc.Needs.Modify(kvp.Key, kvp.Value * dt);
                    }
                }
            }
        }

        private bool HasCriticalNeeds(NPC npc)
        {
            bool isCritical = npc.Needs.IsCritical(NeedType.Hunger) || npc.Needs.IsCritical(NeedType.Energy);

            // Hysteresis for Energy/Sleeping:
            // If the NPC is already sleeping, we consider the need critical until it is fully satisfied (>= 95).
            // This prevents waking up immediately after crossing the 20 barrier.
            if (npc.State == NPCState.Sleeping && !npc.Needs.IsSatisfied(NeedType.Energy))
            {
                return true;
            }

            // Hysteresis for Hunger/Eating:
            // If the NPC is already eating, we consider the need critical until it is fully satisfied (>= 95).
            // This prevents stopping eating immediately after crossing the 20 barrier.
            if (npc.State == NPCState.Eating && !npc.Needs.IsSatisfied(NeedType.Hunger))
            {
                return true;
            }

            return isCritical;
        }

        private void ResolveCriticalNeeds(NPC npc, float dt)
        {
             // Check if we are currently satisfying a critical need
             // If so, and not yet satisfied, STAY here.
             
             // Simple Hysteresis:
             // If Energy is Critical, go to bed.
             // Stay in bed until Energy is Satisfied (95+).
             
             if (npc.Needs.IsCritical(NeedType.Energy) || (npc.State == NPCState.Sleeping && !npc.Needs.IsSatisfied(NeedType.Energy)))
             {
                 var bed = _context.Town.GetBestLocationForNeed(NeedType.Energy);
                 if (bed != null)
                 {
                     MoveTo(npc, bed, dt);
                     if (IsAt(npc, bed))
                     {
                         npc.State = NPCState.Sleeping;
                         // Recovery happens in RecoverNeeds now
                     }
                 }
             }
             else if (npc.Needs.IsCritical(NeedType.Hunger) || (npc.State == NPCState.Eating && !npc.Needs.IsSatisfied(NeedType.Hunger)))
             {
                  // Go to food place and eat until fully satisfied
                  var foodPlace = _context.Town.GetBestLocationForNeed(NeedType.Hunger);
                  if (foodPlace != null)
                  {
                      MoveTo(npc, foodPlace, dt);
                      if (IsAt(npc, foodPlace))
                      {
                          npc.State = NPCState.Eating;
                          // Recovery happens in RecoverNeeds
                      }
                  }
             }
        }

        private void FollowSchedule(NPC npc, float dt)
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
                    MoveTo(npc, target, dt);
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

        private void MoveTo(NPC npc, Location location, float dt)
        {
            float speed = 50f; // px/sec - slower than player (200)

            Vector2 dir = location.Position - npc.Position;
            float distance = dir.Length();
            
            // If already close enough, just snap and stay put.
            // This prevents flickering between Moving and Idle/Action states.
            // Using a threshold (e.g. 1.0f) ensures we catch floating point jitters or very small dt.
            if (distance <= speed * dt || distance < 3.0f)
            {
                 // Snap to target
                npc.Position = location.Position;
                npc.CurrentLocationId = location.Id; // Arrived
                // Do NOT set state here, let the caller decide the state (e.g. Sleeping, Working)
                // If caller wants to move, they called MoveTo. If we are here, we are done moving.
            }
            else
            {
                dir.Normalize();
                npc.Position += dir * speed * dt;
                npc.State = NPCState.Moving;
                npc.CurrentLocationId = null; // In transit
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
