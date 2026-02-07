using System;

namespace JumpAndRun.Core
{
    public class TimeSystem
    {
        public int Minute { get; private set; }
        public int Hour { get; private set; }
        public int Day { get; private set; }
        
        // Configuration
        public float RealSecondsPerGameMinute { get; set; } = 0.1f; // 1 game minute = 0.1 real seconds (default) or 1 sec = 10 mins

        private float _accumulator;

        // Events
        public event Action<int> OnMinuteChanged;
        public event Action<int> OnHourChanged;
        public event Action<int> OnDayChanged;

        public TimeSystem()
        {
            Minute = 0;
            Hour = 6; // Start at 6 AM
            Day = 1;
        }

        public void Update(float deltaTime)
        {
            _accumulator += deltaTime;

            while (_accumulator >= RealSecondsPerGameMinute)
            {
                _accumulator -= RealSecondsPerGameMinute;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            Minute++;
            OnMinuteChanged?.Invoke(Minute);

            if (Minute >= 60)
            {
                Minute = 0;
                AdvanceHour();
            }
        }

        private void AdvanceHour()
        {
            Hour++;
            OnHourChanged?.Invoke(Hour);

            if (Hour >= 24)
            {
                Hour = 0;
                AdvanceDay();
            }
        }

        private void AdvanceDay()
        {
            Day++;
            OnDayChanged?.Invoke(Day);
        }

        public string GetTimeString()
        {
            return $"{Day} - {Hour:D2}:{Minute:D2}";
        }
    }
}
