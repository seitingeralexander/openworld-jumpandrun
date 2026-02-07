using System.Collections.Generic;

namespace JumpAndRun.Simulation
{
    public enum NeedType
    {
        Hunger,
        Energy,
        Social
    }

    public class Needs
    {
        private Dictionary<NeedType, float> _values;

        public Needs()
        {
            _values = new Dictionary<NeedType, float>();
            foreach (NeedType need in System.Enum.GetValues(typeof(NeedType)))
            {
                _values[need] = 100f; // Start fully satisfied
            }
        }

        public float GetValue(NeedType need)
        {
            return _values.ContainsKey(need) ? _values[need] : 0f;
        }

        public void Modify(NeedType need, float amount)
        {
            if (_values.ContainsKey(need))
            {
                _values[need] = Microsoft.Xna.Framework.MathHelper.Clamp(_values[need] + amount, 0f, 100f);
            }
        }

        public bool IsCritical(NeedType need)
        {
            return GetValue(need) < 20f;
        }

        public bool IsSatisfied(NeedType need)
        {
            return GetValue(need) >= 95f; // Hysteresis threshold
        }
    }
}
