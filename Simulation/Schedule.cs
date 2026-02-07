using System.Collections.Generic;

namespace JumpAndRun.Simulation
{
    public enum ScheduleAction
    {
        Idle,
        Sleep,
        Work,
        Eat,
        Socialize,
        Wander
    }

    public class ScheduleBlock
    {
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public ScheduleAction Action { get; set; }
        public string TargetLocationId { get; set; } // Optional location override

        public ScheduleBlock(int start, int end, ScheduleAction action, string locationId = null)
        {
            StartHour = start;
            EndHour = end;
            Action = action;
            TargetLocationId = locationId;
        }

        public bool Contains(int hour)
        {
            if (StartHour <= EndHour)
            {
                return hour >= StartHour && hour < EndHour;
            }
            else
            {
                // Wrap around midnight (e.g., 22:00 to 06:00)
                return hour >= StartHour || hour < EndHour;
            }
        }
    }

    public class Schedule
    {
        public List<ScheduleBlock> Blocks { get; private set; }

        public Schedule()
        {
            Blocks = new List<ScheduleBlock>();
        }

        public void AddBlock(ScheduleBlock block)
        {
            Blocks.Add(block);
        }

        public ScheduleBlock GetBlockForHour(int hour)
        {
            foreach (var block in Blocks)
            {
                if (block.Contains(hour))
                    return block;
            }
            return null; // No schedule = Idle
        }
    }
}
